#!/usr/bin/env python3
"""
mock_robot.py - Mock Robot & Telemetry Simulator matching Libbot_Robot contracts.

Mô phỏng hành vi tương đương robot thật (workflow_engine.py + runtime_gateway.py + process_data.py):
1. Đồng bộ activeMapId và điểm xuất phát (PARK / CHARGER) từ Backend DB, gửi telemetry chuẩn
   cùng trạng thái navigationActive=True, navigationReady=True, navigationModeState="READY".
2. Nhận lệnh TCP `start_workflow` với `initial_context` & `steps`:
   - Thực thi tuần tự các step theo transitions (`PROCESS_DATA`, `MOVE`, `WAIT`, `NOTIFY`, `END`).
   - `PROCESS_DATA` gọi thật API Backend (`GET_BOOK_DETAIL` -> `/api/book-guidance/books/{book_id}`,
     `GET_FACILITY_DETAIL` -> `/api/facility-guidance/facilities/{facility_id}`).
   - `MOVE` nội suy vị trí mượt từ `robot_pose` tới `target` trên bản đồ và tự hoàn tất khi đến nơi
     (hoặc dùng `--manual` để điều khiển kết thúc bằng phím `[c]` / `[f]`).
   - `WAIT` hỗ trợ cả `DURATION`/`TIMEOUT` và `USER_TOUCH` (`submit_event`).
   - `NOTIFY` in nội dung TTS sau khi resolve `${...}` từ context.
3. Gửi đầy đủ HTTP callback về Backend giống robot thật:
   - `POST /api/jobs/{job_id}/execution/events/state`
   - `POST /api/jobs/{job_id}/execution/completed`
4. Trả về `get_execution`, `submit_event`, `cancel_workflow`, `ping`, `preflight_workflow`
   đúng schema của `WorkflowEngine`.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import socket
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
from typing import Any, Dict, List, Optional, Tuple


PLACEHOLDER_RE = re.compile(r"\$\{([a-zA-Z0-9_.-]+)\}")

state_lock = threading.RLock()
running_flag = True
manual_mode = False
step_mode = False
step_auto_continue = False

active_map_id: Optional[str] = None
initial_pose: Dict[str, float] = {"x": 0.025, "y": 0.005, "yaw": 0.0}
robot_pose: Dict[str, float] = {"x": 0.025, "y": 0.005, "yaw": 0.0}
robot_twist: Dict[str, float] = {"linearX": 0.0, "linearY": 0.0, "angularZ": 0.0}
navigation_active = True
navigation_state = "IDLE"
active_goal_id: Optional[str] = None
planned_path: List[Dict[str, float]] = []

executions: Dict[str, Dict[str, Any]] = {}
active_execution_id: Optional[str] = None
active_context: Dict[str, Any] = {}
pending_events: Dict[str, Any] = {}
manual_override: Optional[str] = None  # "complete" | "fail" | None
step_gate_event = threading.Event()
step_gate_waiting = False
step_gate_action: Optional[str] = None  # "next" | "fail" | "auto" | None


def send_http_request(
    url: str,
    method: str = "GET",
    data: Optional[Dict[str, Any]] = None,
    timeout: float = 5.0,
) -> Tuple[int, Any]:
    try:
        body = json.dumps(data, ensure_ascii=False).encode("utf-8") if data is not None else None
        headers = {"Accept": "application/json"}
        if body is not None:
            headers["Content-Type"] = "application/json"
        req = urllib.request.Request(url, data=body, headers=headers, method=method)
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            content = resp.read().decode("utf-8")
            return resp.status, json.loads(content) if content else {}
    except urllib.error.HTTPError as exc:
        content = exc.read().decode("utf-8", errors="ignore")
        try:
            return exc.code, json.loads(content)
        except Exception:
            return exc.code, {"error": content}
    except Exception as exc:
        return 0, {"error": str(exc)}


def resolve_path(context: Dict[str, Any], dotted_key: str) -> Any:
    current: Any = context
    for part in dotted_key.split("."):
        if isinstance(current, dict) and part in current:
            current = current[part]
        else:
            return None
    return current


def resolve_value(context: Dict[str, Any], value: Any) -> Any:
    if isinstance(value, str):
        full_match = PLACEHOLDER_RE.fullmatch(value.strip())
        if full_match:
            resolved = resolve_path(context, full_match.group(1))
            return resolved
        return PLACEHOLDER_RE.sub(
            lambda m: "" if resolve_path(context, m.group(1)) is None else str(resolve_path(context, m.group(1))),
            value,
        )
    if isinstance(value, dict):
        return {k: resolve_value(context, v) for k, v in value.items()}
    if isinstance(value, list):
        return [resolve_value(context, item) for item in value]
    return value


def set_context_key(context: Dict[str, Any], key: str, val: Any) -> None:
    context[key] = val


def apply_output_mapping(context: Dict[str, Any], step_def: Dict[str, Any], step_result: Dict[str, Any]) -> None:
    mapping = step_def.get("output_mapping") or {}
    if not isinstance(mapping, dict):
        return
    for source_path, target_key in mapping.items():
        val = resolve_path(step_result, str(source_path))
        if val is not None and target_key:
            set_context_key(context, str(target_key), val)


def resolve_backend_url(context: Dict[str, Any], fallback_backend_url: str) -> str:
    candidate = str(context.get("backend_base_url") or fallback_backend_url or "").strip().rstrip("/")
    return candidate


def publish_execution_state(
    backend_url: str,
    execution_id: str,
    status: str,
    result: Any = None,
    error: Optional[str] = None,
) -> None:
    with state_lock:
        execution = dict(executions.get(execution_id, {}))
    job_id = execution.get("job_id") or execution.get("task_id")
    if not backend_url or not job_id:
        return
    body = {
        "executionId": execution_id,
        "runtimeId": execution.get("runtime_id") or "real",
        "status": status,
        "stepId": execution.get("current_step"),
        "stepName": execution.get("current_step_name"),
        "stepType": execution.get("current_step_type"),
        "expectedEventType": execution.get("expected_event_type"),
        "allowedValues": execution.get("allowed_values") or [],
        "expectedBookId": execution.get("assigned_book_id"),
        "result": result,
        "error": error,
    }
    send_http_request(f"{backend_url}/api/jobs/{job_id}/execution/events/state", method="POST", data=body)


def report_execution_completion(
    backend_url: str,
    job_id: Any,
    execution_id: str,
    status: str,
    message: Optional[str] = None,
) -> None:
    if not backend_url or not job_id:
        return
    body = {
        "status": status,
        "executionId": execution_id,
        "message": message,
    }
    send_http_request(f"{backend_url}/api/jobs/{job_id}/execution/completed", method="POST", data=body, timeout=10.0)


def sync_map_and_endpoints(backend_url: str) -> None:
    global active_map_id, initial_pose, robot_pose
    print(f"[*] Đang đồng bộ bản đồ từ Backend ({backend_url})...")
    status, data = send_http_request(f"{backend_url.rstrip('/')}/api/robot-runtimes/real/maps/latest")
    if status == 200 and isinstance(data, dict) and data.get("id"):
        active_map_id = data["id"]
        print(f"[*] Map active: '{data.get('name', 'Unknown')}' (ID: {active_map_id})")
        ep_status, endpoints = send_http_request(f"{backend_url.rstrip('/')}/api/maps/{active_map_id}/endpoints")
        if ep_status == 200 and isinstance(endpoints, list):
            start_ep = next(
                (ep for ep in endpoints if ep.get("endpointType") in ("PARK", "CHARGER") and ep.get("isActive")),
                None,
            )
            if start_ep:
                with state_lock:
                    initial_pose = {
                        "x": float(start_ep.get("positionX", 0.0)),
                        "y": float(start_ep.get("positionY", 0.0)),
                        "yaw": float(start_ep.get("theta", 0.0)),
                    }
                    robot_pose = dict(initial_pose)
                print(
                    f"[*] Điểm xuất phát '{start_ep.get('name')}': "
                    f"({initial_pose['x']:.2f}, {initial_pose['y']:.2f})"
                )
    else:
        print("[!] Chưa lấy được map mới nhất từ BE; sẽ thử dùng tọa độ mặc định.")


def execute_process_data(
    step_def: Dict[str, Any],
    context: Dict[str, Any],
    fallback_backend_url: str,
) -> Dict[str, Any]:
    params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
    action = str(params.get("action") or params.get("operation") or "IDENTITY").upper()
    payload = resolve_value(context, params.get("payload") or {})
    if not isinstance(payload, dict):
        payload = {}
    base_url = resolve_backend_url(context, fallback_backend_url)

    try:
        if action == "GET_BOOK_DETAIL":
            book_id = str(payload.get("book_id") or "").strip()
            if not book_id:
                raise ValueError("GET_BOOK_DETAIL requires book_id in context (matching real robot).")
            status, data = send_http_request(f"{base_url}/api/book-guidance/books/{urllib.parse.quote(book_id, safe='')}")
            if status != 200 or not isinstance(data, dict):
                raise ValueError(f"GET /api/book-guidance/books/{book_id} returned HTTP {status}.")
            copies = data.get("bookItems") or []
            selected_item_id = context.get("book_item_id") or payload.get("book_item_id")
            if selected_item_id:
                selected_item = next(
                    (item for item in copies if str(item.get("id", "")).lower() == str(selected_item_id).lower()),
                    None,
                )
            else:
                selected_item = next(
                    (
                        item
                        for item in copies
                        if str(item.get("status", "")).lower() == "available"
                        and isinstance(item.get("location"), dict)
                        and isinstance(item["location"].get("endpoint"), dict)
                    ),
                    None,
                )
            if selected_item is None or str(selected_item.get("status", "")).lower() != "available":
                raise ValueError("No available copy of this book has a navigation endpoint.")
            location = selected_item.get("location")
            endpoint = location.get("endpoint") if isinstance(location, dict) else None
            if not isinstance(endpoint, dict):
                raise ValueError("The selected book copy has no navigation endpoint.")
            pose = [float(endpoint[k]) for k in ("x", "y", "theta")]
            if not all(math.isfinite(v) for v in pose):
                raise ValueError("The selected book copy has an invalid navigation endpoint.")
            data = dict(data)
            data["location"] = location
            data["selectedBookItem"] = selected_item
            return {"status": "processed", "type": "PROCESS_DATA", "action": action, "data": data}

        if action == "GET_FACILITY_DETAIL":
            facility_id = str(payload.get("facility_id") or "").strip()
            if not facility_id:
                raise ValueError("GET_FACILITY_DETAIL requires facility_id in context.")
            status, data = send_http_request(
                f"{base_url}/api/facility-guidance/facilities/{urllib.parse.quote(facility_id, safe='')}"
            )
            if status != 200 or not isinstance(data, dict):
                raise ValueError(f"GET /api/facility-guidance/facilities/{facility_id} returned HTTP {status}.")
            return {"status": "processed", "type": "PROCESS_DATA", "action": action, "data": data}

        if action == "SEARCH_FACILITY":
            q = urllib.parse.urlencode({"query": payload.get("query", ""), "matchMode": payload.get("match_mode", "FUZZY")})
            status, data = send_http_request(f"{base_url}/api/facility-guidance/search?{q}")
            if status != 200 or not isinstance(data, dict):
                raise ValueError(f"SEARCH_FACILITY returned HTTP {status}.")
            return {"status": "processed", "type": "PROCESS_DATA", "action": action, "data": data}

        if action == "SEARCH_BOOK":
            q = urllib.parse.urlencode({"query": payload.get("query", ""), "matchMode": payload.get("match_mode", "FUZZY")})
            status, data = send_http_request(f"{base_url}/api/book-guidance/search?{q}")
            if status != 200 or not isinstance(data, dict):
                raise ValueError(f"SEARCH_BOOK returned HTTP {status}.")
            return {"status": "processed", "type": "PROCESS_DATA", "action": action, "data": data}

        return {"status": "processed", "type": "PROCESS_DATA", "action": action, "data": payload}
    except Exception as exc:
        return {"status": "error", "type": "PROCESS_DATA", "action": action, "message": str(exc)}


def execute_move(
    execution_id: str,
    step_def: Dict[str, Any],
    context: Dict[str, Any],
) -> Dict[str, Any]:
    global robot_pose, robot_twist, navigation_state, active_goal_id, planned_path, manual_override
    params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
    raw_target = resolve_value(context, params.get("target"))
    if not isinstance(raw_target, dict) or "x" not in raw_target or "y" not in raw_target:
        return {
            "status": "error",
            "type": "MOVE",
            "move_status": "FAILED",
            "nav2_result_code": "INVALID_INPUT",
            "message": "MOVE requires x and y coordinates.",
        }

    target = {
        "x": float(raw_target["x"]),
        "y": float(raw_target["y"]),
        "theta": float(raw_target.get("theta", 0.0)),
    }
    with state_lock:
        start_pose = dict(robot_pose)
        planned_path = [
            {"x": start_pose["x"], "y": start_pose["y"]},
            {"x": (start_pose["x"] + target["x"]) / 2.0, "y": (start_pose["y"] + target["y"]) / 2.0},
            {"x": target["x"], "y": target["y"]},
        ]
        navigation_state = "NAVIGATING"
        active_goal_id = execution_id
        robot_twist = {"linearX": 0.25, "linearY": 0.0, "angularZ": 0.0}
        manual_override = None

    print(f"\n[ROBOT MOVE] >>> Đang di chuyển tới ({target['x']:.2f}, {target['y']:.2f})...")
    print("[ROBOT MOVE] >>> Nhấn [c] + Enter để đến đích ngay | [f] + Enter để giả lập lỗi\n> ", end="", flush=True)

    speed_mps = 0.45
    dt = 0.2
    while running_flag:
        with state_lock:
            exec_state = executions.get(execution_id, {})
            if exec_state.get("status") == "cancelled":
                navigation_state = "IDLE"
                active_goal_id = None
                planned_path = []
                robot_twist = {"linearX": 0.0, "linearY": 0.0, "angularZ": 0.0}
                return {
                    "status": "error",
                    "type": "MOVE",
                    "move_status": "CANCELLED",
                    "nav2_result_code": "CANCELED",
                    "message": "Navigation cancelled.",
                }
            if manual_override == "fail":
                manual_override = None
                navigation_state = "IDLE"
                active_goal_id = None
                planned_path = []
                robot_twist = {"linearX": 0.0, "linearY": 0.0, "angularZ": 0.0}
                return {
                    "status": "error",
                    "type": "MOVE",
                    "move_status": "FAILED",
                    "nav2_result_code": "FAILED_TO_MAKE_PROGRESS",
                    "message": "Robot navigation obstacle encounter (simulated).",
                }
            if manual_override == "complete":
                manual_override = None
                robot_pose = {"x": target["x"], "y": target["y"], "yaw": target["theta"]}
                navigation_state = "IDLE"
                active_goal_id = None
                planned_path = []
                robot_twist = {"linearX": 0.0, "linearY": 0.0, "angularZ": 0.0}
                break

            dx = target["x"] - robot_pose["x"]
            dy = target["y"] - robot_pose["y"]
            dist = math.hypot(dx, dy)
            if dist <= 0.05:
                if not manual_mode:
                    robot_pose = {"x": target["x"], "y": target["y"], "yaw": target["theta"]}
                    navigation_state = "IDLE"
                    active_goal_id = None
                    planned_path = []
                    robot_twist = {"linearX": 0.0, "linearY": 0.0, "angularZ": 0.0}
                    break
            else:
                step = min(dist, speed_mps * dt)
                robot_pose["x"] += (dx / dist) * step
                robot_pose["y"] += (dy / dist) * step
                robot_pose["yaw"] = math.atan2(dy, dx)
        time.sleep(dt)

    return {
        "status": "ok",
        "type": "MOVE",
        "move_status": "COMPLETED",
        "target": target,
        "final_pose": dict(robot_pose),
        "nav2_result_code": "SUCCEEDED",
    }


def execute_wait(execution_id: str, step_def: Dict[str, Any], context: Dict[str, Any]) -> Dict[str, Any]:
    params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
    wait_for = str(params.get("wait_for", "DURATION")).upper()
    if wait_for == "USER_TOUCH" and params.get("event_type"):
        event_type = str(params["event_type"]).upper()
        result_var = params.get("result_variable")
        timeout_sec = float(params.get("timeout_sec", 120))
        deadline = time.monotonic() + timeout_sec
        while running_flag and time.monotonic() <= deadline:
            with state_lock:
                if executions.get(execution_id, {}).get("status") == "cancelled":
                    return {"status": "cancelled", "type": "WAIT"}
                if event_type in pending_events:
                    data = pending_events.pop(event_type)
                    if result_var:
                        context[str(result_var)] = data.get(result_var, data) if isinstance(data, dict) else data
                    return {"status": "event", "type": "WAIT", "event_type": event_type, "event_data": data}
            time.sleep(0.1)
        return {"status": "timeout", "type": "WAIT", "event_type": event_type}

    delay = float(params.get("timeout_sec", params.get("seconds", 2)) if wait_for == "TIMEOUT" else params.get("seconds", 2))
    time.sleep(min(max(delay, 0.0), 5.0))
    return {"status": "timeout" if wait_for == "TIMEOUT" else "waited", "type": "WAIT", "seconds": delay}


def execute_notify(step_def: Dict[str, Any], context: Dict[str, Any]) -> Dict[str, Any]:
    params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
    content = resolve_value(context, params.get("content", params.get("message", "")))
    print(f"\n[ROBOT TTS] >>> {content}")
    return {
        "status": "notified",
        "type": "NOTIFY",
        "channel": params.get("channel", "SPEAKER_TTS"),
        "message": content,
        "delivery": "delivered",
    }


def execute_scan(step_def: Dict[str, Any], context: Dict[str, Any]) -> Dict[str, Any]:
    params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
    expected = resolve_value(context, params.get("expected_value"))
    scanned = str(expected).strip() if expected not in (None, "") else "MOCK_BARCODE"
    output_key = str(params.get("output_key") or "").strip()
    if output_key:
        set_context_key(context, output_key, scanned)
    print(f"\n[ROBOT SCAN] >>> Quét {params.get('scan_type', 'BARCODE')}: {scanned}")
    return {
        "status": "ok",
        "type": "SCAN",
        "scan_type": params.get("scan_type", "BARCODE"),
        "scanned_value": scanned,
        "matched": True,
    }


def describe_step_summary(step_type: str, params: Dict[str, Any], context: Dict[str, Any]) -> str:
    if step_type == "PROCESS_DATA":
        return f"action={params.get('action', '')}"
    if step_type == "MOVE":
        target = resolve_value(context, params.get("target"))
        if isinstance(target, dict) and "x" in target and "y" in target:
            return f"target=({float(target['x']):.2f}, {float(target['y']):.2f})"
        return f"target={params.get('target')}"
    if step_type == "WAIT":
        return f"wait_for={params.get('wait_for', 'DURATION')}, sec={params.get('seconds', params.get('timeout_sec', 2))}"
    if step_type == "SCAN":
        return f"expected={resolve_value(context, params.get('expected_value'))}"
    if step_type == "NOTIFY":
        return f"tts=\"{resolve_value(context, params.get('content', params.get('message', '')))}\""
    if step_type == "END":
        return f"result={params.get('result_status', params.get('status', 'SUCCEEDED'))}"
    return ""


def wait_for_step_gate(
    execution_id: str,
    step_num: int,
    total_steps: int,
    step_name: str,
    step_type: str,
    params: Dict[str, Any],
    context: Dict[str, Any],
) -> str:
    global step_gate_waiting, step_gate_action, step_auto_continue
    summary = describe_step_summary(step_type, params, context)
    detail = f" | {summary}" if summary else ""
    if not step_mode or step_auto_continue:
        print(f"\n[STEP {step_num}/{total_steps}] {step_name} ({step_type}){detail}")
        return "next"

    with state_lock:
        step_gate_action = None
        step_gate_waiting = True
        step_gate_event.clear()

    print(f"\n[STEP {step_num}/{total_steps}] TẠM DỪNG TRƯỚC {step_name} ({step_type}){detail}")
    print("[STEP GATE] >>> [Enter] hoặc [n]+Enter: chạy bước này | [a]+Enter: chạy hết phần còn lại | [f]+Enter: giả lập bước LỖI\n> ", end="", flush=True)

    try:
        while running_flag:
            with state_lock:
                if executions.get(execution_id, {}).get("status") == "cancelled":
                    return "cancelled"
                if step_gate_action is not None:
                    action = step_gate_action
                    step_gate_action = None
                    if action == "auto":
                        step_auto_continue = True
                        return "next"
                    return action
            step_gate_event.wait(timeout=0.15)
        return "cancelled"
    finally:
        with state_lock:
            step_gate_waiting = False
            step_gate_event.clear()


def run_workflow_thread(
    execution_id: str,
    task_id: Any,
    steps: List[Dict[str, Any]],
    context: Dict[str, Any],
    fallback_backend_url: str,
) -> None:
    global active_execution_id, step_auto_continue
    backend_url = resolve_backend_url(context, fallback_backend_url)
    indexed = {str(step.get("id", step.get("name", i))): (i, step) for i, step in enumerate(steps)}
    results: List[Dict[str, Any]] = []
    failed_step: Optional[str] = None
    failure_message: Optional[str] = None
    index = 0
    step_auto_continue = False

    with state_lock:
        executions[execution_id]["status"] = "running"

    while running_flag and 0 <= index < len(steps):
        with state_lock:
            if executions.get(execution_id, {}).get("status") == "cancelled":
                break
        step_def = steps[index]
        step_id = str(step_def.get("id", step_def.get("name", f"step-{index + 1}")))
        step_name = str(step_def.get("name", step_id))
        step_type = str(step_def.get("type", "")).upper()
        params = step_def.get("params") if isinstance(step_def.get("params"), dict) else step_def
        waits_for_event = step_type == "WAIT" and str(params.get("wait_for", "")).upper() == "USER_TOUCH" and bool(params.get("event_type"))

        with state_lock:
            executions[execution_id].update({
                "current_step": step_id,
                "current_step_name": step_name,
                "current_step_type": step_type,
                "expected_event_type": str(params.get("event_type")).upper() if waits_for_event else None,
                "allowed_values": list(params.get("allowed_values") or []) if waits_for_event else [],
            })
        publish_execution_state(backend_url, execution_id, "WAITING" if waits_for_event else "RUNNING")

        gate_decision = wait_for_step_gate(
            execution_id, index + 1, len(steps), step_name, step_type, params, context
        )
        if gate_decision == "cancelled":
            break
        if gate_decision == "fail":
            step_result = {"status": "error", "type": step_type, "message": f"Simulated failure at step {step_name} (--step)."}
        elif step_type == "PROCESS_DATA":
            step_result = execute_process_data(step_def, context, backend_url)
        elif step_type == "MOVE":
            step_result = execute_move(execution_id, step_def, context)
        elif step_type == "WAIT":
            step_result = execute_wait(execution_id, step_def, context)
        elif step_type == "SCAN":
            step_result = execute_scan(step_def, context)
        elif step_type == "NOTIFY":
            step_result = execute_notify(step_def, context)
        elif step_type == "END":
            res_status = str(params.get("result_status", params.get("status", "SUCCEEDED"))).upper()
            step_result = {"status": "ok" if res_status == "SUCCEEDED" else "error", "type": "END", "workflow_status": res_status}
        else:
            step_result = {"status": "ok", "type": step_type}

        apply_output_mapping(context, step_def, step_result)
        succeeded = (
            step_result.get("move_status") == "COMPLETED" if step_type == "MOVE"
            else step_result.get("workflow_status") == "SUCCEEDED" if step_type == "END"
            else step_result.get("status") not in {"error", "failed", "timeout", "cancelled"}
        )
        results.append({"step": step_name, "step_type": step_type, "result": step_result})
        publish_execution_state(
            backend_url,
            execution_id,
            "STEP_COMPLETED" if succeeded else "STEP_FAILED",
            result=step_result,
            error=None if succeeded else step_result.get("message"),
        )

        if step_type == "END":
            if step_result.get("workflow_status") != "SUCCEEDED":
                failed_step = step_name
                failure_message = step_result.get("message") or "Workflow ended with FAILED."
            break

        transitions = step_def.get("transitions") if isinstance(step_def.get("transitions"), dict) else {}
        if step_type == "WAIT" and step_result.get("status") == "event":
            next_id = transitions.get("on_event", transitions.get("on_success"))
        elif succeeded:
            next_id = transitions.get("on_success")
        else:
            failed_step = failed_step or step_name
            failure_message = failure_message or step_result.get("message") or f"Step {step_name} failed."
            next_id = transitions.get("on_error")
            if next_id is None:
                break

        if next_id is not None and str(next_id) in indexed:
            index = indexed[str(next_id)][0]
        else:
            if not succeeded:
                break
            index += 1

    with state_lock:
        was_cancelled = executions.get(execution_id, {}).get("status") == "cancelled"
        final_status = "cancelled" if was_cancelled else "failed" if failed_step else "completed"
        result_bundle = {
            "status": "cancelled" if was_cancelled else "error" if failed_step else "ok",
            "task_id": task_id,
            "failed_step": failed_step,
            "error": failure_message,
            "results": results,
        }
        executions[execution_id].update({
            "status": final_status,
            "expected_event_type": None,
            "allowed_values": [],
            "result": result_bundle,
            "error": failure_message,
        })
        if active_execution_id == execution_id:
            active_execution_id = None

    publish_execution_state(backend_url, execution_id, final_status.upper(), result=result_bundle, error=failure_message)
    cb_status = "CANCELLED" if was_cancelled else "FAILED" if failed_step else "SUCCEEDED"
    report_execution_completion(backend_url, task_id, execution_id, cb_status, failure_message)
    print(f"\n[ROBOT WORKFLOW] >>> Hoàn tất execution {execution_id}: {final_status.upper()}")
    print("> ", end="", flush=True)


def telemetry_loop(backend_url: str, runtime_id: str) -> None:
    while running_flag:
        try:
            with state_lock:
                payload = {
                    "timestamp": time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime()),
                    "pose": dict(robot_pose),
                    "twist": dict(robot_twist),
                    "battery": 95.0,
                    "localizationState": "LOCALIZED",
                    "navigationState": navigation_state,
                    "activeGoalId": active_goal_id,
                    "plannedPath": list(planned_path),
                    "error": None,
                    "mappingActive": False,
                    "navigationActive": navigation_active,
                    "navigationReady": True,
                    "navigationModeState": "READY" if navigation_active else "STOPPED",
                    "activeMapId": active_map_id,
                }
            send_http_request(f"{backend_url.rstrip('/')}/api/robot-runtimes/{runtime_id}/telemetry", method="POST", data=payload, timeout=2.0)
        except Exception:
            pass
        time.sleep(0.5)


def handle_tcp_client(conn: socket.socket, backend_url: str) -> None:
    global active_execution_id, active_context, navigation_active
    with conn:
        buffer = ""
        while running_flag:
            try:
                chunk = conn.recv(4096)
            except OSError:
                break
            if not chunk:
                break
            buffer += chunk.decode("utf-8", errors="ignore")
            while "\n" in buffer:
                line, buffer = buffer.split("\n", 1)
                line = line.strip()
                if not line:
                    continue
                try:
                    req = json.loads(line)
                    cmd = str(req.get("command", "")).lower()
                    if cmd in ("ping", "health"):
                        resp = {"status": "ok", "message": "libot mock workflow engine is running"}
                    elif cmd == "preflight_workflow":
                        resp = {"status": "ok", "checks": {"navigation": "ready", "speaker": "ready"}}
                    elif cmd == "start_workflow":
                        steps = req.get("steps")
                        init_ctx = req.get("initial_context") or {}
                        exec_id = str(req.get("execution_id") or req.get("task_id") or "")
                        task_id = req.get("task_id")
                        if not isinstance(steps, list) or not steps:
                            resp = {"status": "error", "message": "start_workflow requires a steps array."}
                        else:
                            with state_lock:
                                if active_execution_id and executions.get(active_execution_id, {}).get("status") in ("accepted", "running"):
                                    resp = {"status": "error", "message": "The robot is already running another workflow."}
                                else:
                                    active_context = dict(init_ctx)
                                    pending_events.clear()
                                    active_execution_id = exec_id
                                    executions[exec_id] = {
                                        "status": "accepted",
                                        "task_id": task_id,
                                        "job_id": task_id,
                                        "runtime_id": init_ctx.get("runtime_id", "real"),
                                        "assigned_book_id": init_ctx.get("book_id"),
                                        "current_step": None,
                                        "current_step_name": None,
                                        "current_step_type": None,
                                        "expected_event_type": None,
                                        "allowed_values": [],
                                    }
                                    resp = {"status": "accepted", "execution_id": exec_id, "task_id": task_id}
                            if resp.get("status") == "accepted":
                                publish_execution_state(resolve_backend_url(active_context, backend_url), exec_id, "ACCEPTED")
                                threading.Thread(
                                    target=run_workflow_thread,
                                    args=(exec_id, task_id, steps, active_context, backend_url),
                                    daemon=True,
                                ).start()
                    elif cmd == "get_execution":
                        exec_id = str(req.get("execution_id") or "")
                        with state_lock:
                            execution = executions.get(exec_id)
                            if execution is None:
                                resp = {"status": "error", "message": f"Execution {exec_id or '<missing>'} was not found."}
                            else:
                                snap = dict(execution)
                                snap["currentStep"] = snap.pop("current_step", None)
                                snap["currentStepName"] = snap.pop("current_step_name", None)
                                snap["currentStepType"] = snap.pop("current_step_type", None)
                                snap["expectedEventType"] = snap.pop("expected_event_type", None)
                                snap["allowedValues"] = snap.pop("allowed_values", [])
                                snap["expectedBookId"] = snap.pop("assigned_book_id", None)
                                resp = {"status": "ok", "execution_id": exec_id, "execution": snap}
                    elif cmd == "submit_event":
                        ev_type = str(req.get("event_type") or "").upper()
                        exec_id = str(req.get("execution_id") or "")
                        with state_lock:
                            execution = executions.get(exec_id)
                            if not execution or execution.get("status") not in ("accepted", "running"):
                                resp = {"status": "error", "code": "NO_ACTIVE_EXECUTION", "message": "There is no active workflow waiting for an event."}
                            elif execution.get("expected_event_type") != ev_type:
                                resp = {"status": "error", "code": "UNEXPECTED_EVENT", "message": f"Expected '{execution.get('expected_event_type')}', got '{ev_type}'."}
                            else:
                                pending_events[ev_type] = req.get("data")
                                resp = {"status": "accepted", "execution_id": exec_id, "event_type": ev_type}
                    elif cmd == "cancel_workflow":
                        exec_id = str(req.get("execution_id") or "")
                        with state_lock:
                            if exec_id in executions:
                                executions[exec_id]["status"] = "cancelled"
                                resp = {"status": "accepted", "execution_id": exec_id}
                            else:
                                resp = {"status": "error", "message": "Workflow execution was not found."}
                    elif cmd == "start_navigation_mode":
                        navigation_active = True
                        resp = {"status": "accepted", "zone_count": 0, "zone_counts_by_type": {}}
                    elif cmd == "stop_navigation_mode":
                        navigation_active = False
                        resp = {"status": "ok"}
                    elif cmd == "update_navigation_zones":
                        resp = {"status": "ok"}
                    else:
                        resp = {"status": "ok", "message": f"Mock handled '{cmd}'"}
                    conn.sendall((json.dumps(resp, ensure_ascii=False) + "\n").encode("utf-8"))
                except Exception as exc:
                    conn.sendall((json.dumps({"status": "error", "message": str(exc)}) + "\n").encode("utf-8"))


def tcp_server_loop(host: str, port: int, backend_url: str) -> None:
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        srv.bind((host, port))
        srv.listen(5)
        print(f"[*] TCP Server đang lắng nghe tại {host}:{port}")
        while running_flag:
            try:
                srv.settimeout(1.0)
                conn, _ = srv.accept()
                threading.Thread(target=handle_tcp_client, args=(conn, backend_url), daemon=True).start()
            except socket.timeout:
                continue
    finally:
        srv.close()


def cli_interactive_loop() -> None:
    global manual_override, running_flag, robot_pose, navigation_state, planned_path, step_gate_action
    print("\n" + "=" * 66)
    print(" LIBOT ROBOT MOCK CONTROLLER (CONTRACT MATCHED WITH REAL ROBOT)")
    print("=" * 66)
    move_label = "THỦ CÔNG (--manual)" if manual_mode else "TỰ ĐỘNG ĐẾN ĐÍCH"
    step_label = "TỪNG BƯỚC (--step)" if step_mode else "CHẠY LIÊN TỤC"
    print(f"  Chế độ di chuyển : {move_label}")
    print(f"  Chế độ workflow  : {step_label}")
    if step_mode:
        print("  [Enter] / [n]    : Chạy tiếp 1 bước đang tạm dừng")
        print("  [a] + Enter      : Chạy nốt phần còn lại không dừng")
    print("  [c] + Enter      : Nhảy tới đích & hoàn tất MOVE ngay")
    print("  [f] + Enter      : Giả lập LỖI (bước hiện tại / Nav2 MOVE)")
    print("  [r] + Enter      : Reset vị trí robot về điểm xuất phát")
    print("  [q] + Enter      : Thoát mock")
    print("=" * 66 + "\n> ", end="", flush=True)

    while running_flag:
        try:
            line = sys.stdin.readline()
            if not line:
                break
            cmd = line.strip().lower()
            if cmd in ("", "n"):
                with state_lock:
                    if step_gate_waiting:
                        step_gate_action = "next"
                        step_gate_event.set()
            elif cmd == "a":
                with state_lock:
                    if step_gate_waiting:
                        step_gate_action = "auto"
                        step_gate_event.set()
            elif cmd == "c":
                with state_lock:
                    if step_gate_waiting:
                        step_gate_action = "next"
                        step_gate_event.set()
                    else:
                        manual_override = "complete"
            elif cmd == "f":
                with state_lock:
                    if step_gate_waiting:
                        step_gate_action = "fail"
                        step_gate_event.set()
                    else:
                        manual_override = "fail"
            elif cmd == "r":
                with state_lock:
                    robot_pose = dict(initial_pose)
                    navigation_state = "IDLE"
                    planned_path = []
                print("[MOCK] >>> Đã reset vị trí robot về điểm xuất phát.")
            elif cmd == "q":
                running_flag = False
                step_gate_event.set()
                break
        except (KeyboardInterrupt, EOFError):
            running_flag = False
            step_gate_event.set()
            break


def main() -> None:
    global manual_mode, step_mode
    parser = argparse.ArgumentParser(description="LiBot Mock Robot Simulator")
    parser.add_argument("--host", default="0.0.0.0", help="TCP host (default 0.0.0.0)")
    parser.add_argument("--port", type=int, default=9000, help="TCP port (default 9000)")
    parser.add_argument("--backend", default=os.getenv("LIBOT_BACKEND_URL", "http://127.0.0.1:5205"), help="Backend API URL")
    parser.add_argument("--runtime", default=os.getenv("LIBOT_RUNTIME_ID", "real"), help="Runtime id (default real)")
    parser.add_argument("--manual", action="store_true", help="Chờ nhấn [c] thay vì tự hoàn tất khi MOVE tới đích")
    parser.add_argument("--step", action="store_true", help="Tạm dừng trước từng bước workflow (nhấn Enter/[n] để đi tiếp)")
    args = parser.parse_args()
    manual_mode = args.manual
    step_mode = args.step

    sync_map_and_endpoints(args.backend)
    threading.Thread(target=tcp_server_loop, args=(args.host, args.port, args.backend), daemon=True).start()
    threading.Thread(target=telemetry_loop, args=(args.backend, args.runtime), daemon=True).start()
    cli_interactive_loop()


if __name__ == "__main__":
    main()

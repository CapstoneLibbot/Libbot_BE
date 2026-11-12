import { readFileSync } from 'node:fs';

const path = process.argv[2] ?? 'docs/database/Logical_ERD.corrected.json';
const document = JSON.parse(readFileSync(path, 'utf8'));
const cells = document.pages?.[0]?.cells ?? [];
const byId = new Map(cells.map(cell => [cell.id, cell]));
const children = new Map();
for (const cell of cells) (children.get(cell.parent) ?? children.set(cell.parent, []).get(cell.parent)).push(cell);
const tables = new Set((children.get('1') ?? [])
  .filter(cell => cell.type === 'node' && cell.label && !cell.label.startsWith('NOTE:'))
  .map(cell => cell.id));

function lineage(id) {
  const result = [];
  for (let cell = byId.get(id); cell; cell = byId.get(cell.parent)) result.push(cell);
  return result;
}
function descendants(id) {
  return (children.get(id) ?? []).flatMap(cell => [cell, ...descendants(cell.id)]);
}
function isKeyEndpoint(id, kind) {
  const chain = lineage(id);
  const tableIndex = chain.findIndex(cell => tables.has(cell.id));
  const row = tableIndex > 0 ? chain[tableIndex - 1] : undefined;
  return row ? descendants(row.id).some(cell => cell.label === kind) : false;
}
const tableFor = id => lineage(id).find(cell => tables.has(cell.id));
const errors = [];
for (const edge of cells.filter(cell => cell.type === 'edge')) {
  const sourceTable = tableFor(edge.source);
  const targetTable = tableFor(edge.target);
  if (!sourceTable || !targetTable) errors.push(`Edge ${edge.id} is not between table fields.`);
  if (!isKeyEndpoint(edge.source, 'PK')) errors.push(`Edge ${edge.id} source is not a PK field.`);
  if (!isKeyEndpoint(edge.target, 'FK')) errors.push(`Edge ${edge.id} target is not an FK field.`);
}
if ((children.get('1') ?? []).some(cell => cell.label === 'account_id')) errors.push('Orphan account_id table remains.');
if (errors.length) throw new Error(errors.join('\n'));
console.log(`Validated ${tables.size} tables and ${cells.filter(cell => cell.type === 'edge').length} PK-to-FK connectors.`);

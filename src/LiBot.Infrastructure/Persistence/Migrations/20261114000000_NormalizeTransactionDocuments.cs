using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations;

/// <summary>Makes receipt and issue headers multi-book documents and enforces detail consistency.</summary>
[Migration("20261114000000_NormalizeTransactionDocuments")]
public partial class NormalizeTransactionDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM dbo.BOOK_RECEIPT_DETAIL GROUP BY ReceiptId, BookItemId HAVING COUNT(*) > 1)
    THROW 51010, 'A book item appears more than once in a receipt.', 1;
IF EXISTS (SELECT 1 FROM dbo.BOOK_ISSUE_DETAIL GROUP BY IssueId, BookItemId HAVING COUNT(*) > 1)
    THROW 51011, 'A book item appears more than once in an issue.', 1;
IF EXISTS (SELECT 1 FROM dbo.BOOK_RECEIPT_DETAIL d JOIN dbo.BOOK_ITEM i ON i.Id=d.BookItemId WHERE i.BookId<>d.BookId)
    THROW 51012, 'A receipt detail book item does not belong to its stated book.', 1;
IF EXISTS (SELECT 1 FROM dbo.BOOK_ISSUE_DETAIL d JOIN dbo.BOOK_ITEM i ON i.Id=d.BookItemId WHERE i.BookId<>d.BookId)
    THROW 51013, 'An issue detail book item does not belong to its stated book.', 1;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BOOK_RECEIPT_BOOK_BookId')
    ALTER TABLE dbo.BOOK_RECEIPT DROP CONSTRAINT FK_BOOK_RECEIPT_BOOK_BookId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BOOK_ISSUE_BOOK_BookId')
    ALTER TABLE dbo.BOOK_ISSUE DROP CONSTRAINT FK_BOOK_ISSUE_BOOK_BookId;
IF COL_LENGTH(N'dbo.BOOK_RECEIPT', N'BookId') IS NOT NULL ALTER TABLE dbo.BOOK_RECEIPT DROP COLUMN BookId;
IF COL_LENGTH(N'dbo.BOOK_ISSUE', N'BookId') IS NOT NULL ALTER TABLE dbo.BOOK_ISSUE DROP COLUMN BookId;

ALTER TABLE dbo.BOOK_ITEM ADD CONSTRAINT UQ_BOOK_ITEM_Id_BookId UNIQUE (Id, BookId);
ALTER TABLE dbo.BOOK_RECEIPT_DETAIL ADD CONSTRAINT UQ_BOOK_RECEIPT_DETAIL_ReceiptId_BookItemId UNIQUE (ReceiptId, BookItemId);
ALTER TABLE dbo.BOOK_ISSUE_DETAIL ADD CONSTRAINT UQ_BOOK_ISSUE_DETAIL_IssueId_BookItemId UNIQUE (IssueId, BookItemId);

ALTER TABLE dbo.BOOK_RECEIPT_DETAIL DROP CONSTRAINT FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId;
ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP CONSTRAINT FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId;
ALTER TABLE dbo.BOOK_RECEIPT_DETAIL ADD CONSTRAINT FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId_BookId
    FOREIGN KEY (BookItemId, BookId) REFERENCES dbo.BOOK_ITEM(Id, BookId) ON DELETE NO ACTION;
ALTER TABLE dbo.BOOK_ISSUE_DETAIL ADD CONSTRAINT FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId_BookId
    FOREIGN KEY (BookItemId, BookId) REFERENCES dbo.BOOK_ITEM(Id, BookId) ON DELETE NO ACTION;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE dbo.BOOK_RECEIPT_DETAIL DROP CONSTRAINT FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId_BookId;
ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP CONSTRAINT FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId_BookId;
ALTER TABLE dbo.BOOK_RECEIPT_DETAIL DROP CONSTRAINT UQ_BOOK_RECEIPT_DETAIL_ReceiptId_BookItemId;
ALTER TABLE dbo.BOOK_ISSUE_DETAIL DROP CONSTRAINT UQ_BOOK_ISSUE_DETAIL_IssueId_BookItemId;
ALTER TABLE dbo.BOOK_ITEM DROP CONSTRAINT UQ_BOOK_ITEM_Id_BookId;
ALTER TABLE dbo.BOOK_RECEIPT ADD BookId uniqueidentifier NULL;
ALTER TABLE dbo.BOOK_ISSUE ADD BookId uniqueidentifier NULL;
ALTER TABLE dbo.BOOK_RECEIPT_DETAIL ADD CONSTRAINT FK_BOOK_RECEIPT_DETAIL_BOOK_ITEM_BookItemId FOREIGN KEY (BookItemId) REFERENCES dbo.BOOK_ITEM(Id) ON DELETE NO ACTION;
ALTER TABLE dbo.BOOK_ISSUE_DETAIL ADD CONSTRAINT FK_BOOK_ISSUE_DETAIL_BOOK_ITEM_BookItemId FOREIGN KEY (BookItemId) REFERENCES dbo.BOOK_ITEM(Id) ON DELETE NO ACTION;
""");
    }
}

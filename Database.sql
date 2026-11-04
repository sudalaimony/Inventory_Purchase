/* =========================================================
   Inventory Purchase Form - Database Script
   Run this full script once in SQL Server Management Studio
   ========================================================= */

CREATE DATABASE InventoryDB
GO

USE InventoryDB
GO

/* =========================================================
   TABLES
   ========================================================= */

CREATE TABLE Category
(
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE
)
GO

CREATE TABLE Supplier
(
    SupplierId   INT IDENTITY(1,1) PRIMARY KEY,
    SupplierName VARCHAR(150) NOT NULL,
    Phone        VARCHAR(20) NULL,
    Address      VARCHAR(250) NULL,
    IsActive     BIT NOT NULL DEFAULT 1
)
GO

-- TaxType = 'Single' or 'Compound'
-- Compound: AdditionalRate is charged on (taxable amount + first tax)
CREATE TABLE Tax
(
    TaxId          INT IDENTITY(1,1) PRIMARY KEY,
    TaxName        VARCHAR(100) NOT NULL UNIQUE,
    TaxType        VARCHAR(10) NOT NULL DEFAULT 'Single',
    TaxRate        DECIMAL(5,2) NOT NULL,
    AdditionalRate DECIMAL(5,2) NOT NULL DEFAULT 0,
    IsActive       BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_Tax_Type CHECK (TaxType IN ('Single', 'Compound')),
    CONSTRAINT CK_Tax_Rate CHECK (TaxRate >= 0 AND AdditionalRate >= 0)
)
GO

CREATE TABLE Product
(
    ProductId    INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode  VARCHAR(30) NOT NULL UNIQUE,
    ProductName  VARCHAR(150) NOT NULL,
    CategoryId   INT NOT NULL REFERENCES Category(CategoryId),
    TaxId        INT NULL REFERENCES Tax(TaxId),
    CostPrice    DECIMAL(18,2) NOT NULL DEFAULT 0,
    SellingPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    HasExpiry    BIT NOT NULL DEFAULT 1,
    IsActive     BIT NOT NULL DEFAULT 1
)
GO

-- Status = 'Draft' or 'Posted'
CREATE TABLE PurchaseHeader
(
    PurchaseId     INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId     INT NOT NULL REFERENCES Supplier(SupplierId),
    InvoiceNo      VARCHAR(50) NOT NULL,
    InvoiceDate    DATE NOT NULL,
    GrossAmount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status         VARCHAR(10) NOT NULL DEFAULT 'Draft',
    CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
    PostedOn       DATETIME NULL,
    CONSTRAINT UQ_Purchase_Invoice UNIQUE (SupplierId, InvoiceNo),
    CONSTRAINT CK_Purchase_Status CHECK (Status IN ('Draft', 'Posted'))
)
GO

-- Tax name / type / rates are copied here, so old purchases keep the tax used at that time
CREATE TABLE PurchaseDetail
(
    DetailId        INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseId      INT NOT NULL REFERENCES PurchaseHeader(PurchaseId) ON DELETE CASCADE,
    ProductId       INT NOT NULL REFERENCES Product(ProductId),
    LotNo           VARCHAR(50) NOT NULL,
    ExpiryDate      DATE NULL,
    Quantity        DECIMAL(18,2) NOT NULL,
    Rate            DECIMAL(18,2) NOT NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    GrossAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxableAmount   DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxId           INT NULL REFERENCES Tax(TaxId),
    TaxName         VARCHAR(100) NULL,
    TaxType         VARCHAR(10) NULL,
    TaxRate         DECIMAL(5,2) NOT NULL DEFAULT 0,
    AdditionalRate  DECIMAL(5,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Detail_ProductLot UNIQUE (PurchaseId, ProductId, LotNo),
    CONSTRAINT CK_Detail_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_Detail_Rate CHECK (Rate >= 0),
    CONSTRAINT CK_Detail_Discount CHECK (DiscountPercent BETWEEN 0 AND 100)
)
GO

-- One row per Product + Lot. Rows are inserted only when a purchase is posted.
CREATE TABLE Stock
(
    StockId      INT IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT NOT NULL REFERENCES Product(ProductId),
    LotNo        VARCHAR(50) NOT NULL,
    ExpiryDate   DATE NULL,
    Quantity     DECIMAL(18,2) NOT NULL,
    PurchaseRate DECIMAL(18,2) NOT NULL,
    PurchaseId   INT NOT NULL REFERENCES PurchaseHeader(PurchaseId),
    ReceivedOn   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Stock_ProductLot UNIQUE (ProductId, LotNo),
    CONSTRAINT CK_Stock_Qty CHECK (Quantity >= 0)
)
GO

-- Table type used to send all purchase lines to the procedure in one call
CREATE TYPE PurchaseDetailType AS TABLE
(
    RowNo           INT,
    ProductId       INT,
    LotNo           VARCHAR(50),
    ExpiryDate      DATE NULL,
    Quantity        DECIMAL(18,2),
    Rate            DECIMAL(18,2),
    DiscountPercent DECIMAL(5,2)
)
GO

/* =========================================================
   CATEGORY PROCEDURES
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName FROM Category ORDER BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE sp_Category_Insert
    @CategoryName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Category WHERE CategoryName = @CategoryName)
        THROW 50001, 'Category already exists', 1;

    INSERT INTO Category (CategoryName) VALUES (@CategoryName);
END
GO

CREATE OR ALTER PROCEDURE sp_Category_Delete
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Product WHERE CategoryId = @CategoryId)
        THROW 50001, 'Category is used in products. It cannot be deleted.', 1;

    DELETE FROM Category WHERE CategoryId = @CategoryId;
END
GO

/* =========================================================
   SUPPLIER PROCEDURES
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Supplier_GetAll
    @OnlyActive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SupplierId, SupplierName, Phone, Address, IsActive
    FROM Supplier
    WHERE @OnlyActive = 0 OR IsActive = 1
    ORDER BY SupplierName;
END
GO

CREATE OR ALTER PROCEDURE sp_Supplier_GetById
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SupplierId, SupplierName, Phone, Address, IsActive
    FROM Supplier
    WHERE SupplierId = @SupplierId;
END
GO

CREATE OR ALTER PROCEDURE sp_Supplier_Save
    @SupplierId   INT,
    @SupplierName VARCHAR(150),
    @Phone        VARCHAR(20),
    @Address      VARCHAR(250),
    @IsActive     BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF @SupplierId = 0
    BEGIN
        INSERT INTO Supplier (SupplierName, Phone, Address, IsActive)
        VALUES (@SupplierName, @Phone, @Address, @IsActive);
    END
    ELSE
    BEGIN
        UPDATE Supplier
        SET SupplierName = @SupplierName,
            Phone = @Phone,
            Address = @Address,
            IsActive = @IsActive
        WHERE SupplierId = @SupplierId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_Supplier_Delete
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM PurchaseHeader WHERE SupplierId = @SupplierId)
        THROW 50001, 'Supplier has purchases. Make it inactive instead of deleting.', 1;

    DELETE FROM Supplier WHERE SupplierId = @SupplierId;
END
GO

/* =========================================================
   TAX PROCEDURES
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Tax_GetAll
    @OnlyActive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TaxId, TaxName, TaxType, TaxRate, AdditionalRate, IsActive
    FROM Tax
    WHERE @OnlyActive = 0 OR IsActive = 1
    ORDER BY TaxName;
END
GO

CREATE OR ALTER PROCEDURE sp_Tax_GetById
    @TaxId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TaxId, TaxName, TaxType, TaxRate, AdditionalRate, IsActive
    FROM Tax
    WHERE TaxId = @TaxId;
END
GO

CREATE OR ALTER PROCEDURE sp_Tax_Save
    @TaxId          INT,
    @TaxName        VARCHAR(100),
    @TaxType        VARCHAR(10),
    @TaxRate        DECIMAL(5,2),
    @AdditionalRate DECIMAL(5,2),
    @IsActive       BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF @TaxType NOT IN ('Single', 'Compound')
        THROW 50001, 'Select tax type', 1;

    IF @TaxType = 'Single'
        SET @AdditionalRate = 0;

    IF @TaxType = 'Compound' AND @AdditionalRate <= 0
        THROW 50001, 'Enter additional rate for compound tax', 1;

    IF EXISTS (SELECT 1 FROM Tax WHERE TaxName = @TaxName AND TaxId <> @TaxId)
        THROW 50001, 'Tax name already exists', 1;

    IF @TaxId = 0
    BEGIN
        INSERT INTO Tax (TaxName, TaxType, TaxRate, AdditionalRate, IsActive)
        VALUES (@TaxName, @TaxType, @TaxRate, @AdditionalRate, @IsActive);
    END
    ELSE
    BEGIN
        UPDATE Tax
        SET TaxName = @TaxName,
            TaxType = @TaxType,
            TaxRate = @TaxRate,
            AdditionalRate = @AdditionalRate,
            IsActive = @IsActive
        WHERE TaxId = @TaxId;
    END
END
GO

-- If tax is already used, it is only made inactive (not deleted)
CREATE OR ALTER PROCEDURE sp_Tax_Delete
    @TaxId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM PurchaseDetail WHERE TaxId = @TaxId)
       OR EXISTS (SELECT 1 FROM Product WHERE TaxId = @TaxId)
    BEGIN
        UPDATE Tax SET IsActive = 0 WHERE TaxId = @TaxId;
        SELECT 'Tax is already used, so it is marked as inactive instead of deleting.' AS Result;
        RETURN;
    END

    DELETE FROM Tax WHERE TaxId = @TaxId;
    SELECT 'Tax deleted' AS Result;
END
GO

/* =========================================================
   PRODUCT PROCEDURES
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Product_GetAll
    @OnlyActive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductId, p.ProductCode, p.ProductName, p.CategoryId, c.CategoryName,
           p.TaxId, t.TaxName, t.TaxType, t.TaxRate, t.AdditionalRate,
           p.CostPrice, p.SellingPrice, p.HasExpiry, p.IsActive
    FROM Product p
    INNER JOIN Category c ON c.CategoryId = p.CategoryId
    LEFT JOIN Tax t ON t.TaxId = p.TaxId
    WHERE @OnlyActive = 0 OR p.IsActive = 1
    ORDER BY p.ProductCode;
END
GO

CREATE OR ALTER PROCEDURE sp_Product_GetById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProductId, ProductCode, ProductName, CategoryId, TaxId,
           CostPrice, SellingPrice, HasExpiry, IsActive
    FROM Product
    WHERE ProductId = @ProductId;
END
GO

CREATE OR ALTER PROCEDURE sp_Product_Save
    @ProductId    INT,
    @ProductCode  VARCHAR(30),
    @ProductName  VARCHAR(150),
    @CategoryId   INT,
    @TaxId        INT,
    @CostPrice    DECIMAL(18,2),
    @SellingPrice DECIMAL(18,2),
    @HasExpiry    BIT,
    @IsActive     BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Product WHERE ProductCode = @ProductCode AND ProductId <> @ProductId)
        THROW 50001, 'Product code already exists', 1;

    IF @ProductId = 0
    BEGIN
        INSERT INTO Product (ProductCode, ProductName, CategoryId, TaxId, CostPrice, SellingPrice, HasExpiry, IsActive)
        VALUES (@ProductCode, @ProductName, @CategoryId, @TaxId, @CostPrice, @SellingPrice, @HasExpiry, @IsActive);
    END
    ELSE
    BEGIN
        UPDATE Product
        SET ProductCode = @ProductCode,
            ProductName = @ProductName,
            CategoryId = @CategoryId,
            TaxId = @TaxId,
            CostPrice = @CostPrice,
            SellingPrice = @SellingPrice,
            HasExpiry = @HasExpiry,
            IsActive = @IsActive
        WHERE ProductId = @ProductId;
    END
END
GO

-- Product used in any transaction cannot be deleted
CREATE OR ALTER PROCEDURE sp_Product_Delete
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM PurchaseDetail WHERE ProductId = @ProductId)
       OR EXISTS (SELECT 1 FROM Stock WHERE ProductId = @ProductId)
        THROW 50001, 'Product is used in transactions. It cannot be deleted, make it inactive.', 1;

    DELETE FROM Product WHERE ProductId = @ProductId;
END
GO

/* =========================================================
   PURCHASE PROCEDURES
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Purchase_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.PurchaseId, h.SupplierId, s.SupplierName, h.InvoiceNo, h.InvoiceDate,
           h.GrossAmount, h.DiscountAmount, h.TaxAmount, h.NetAmount,
           h.Status, h.CreatedOn, h.PostedOn
    FROM PurchaseHeader h
    INNER JOIN Supplier s ON s.SupplierId = h.SupplierId
    ORDER BY h.PurchaseId DESC;
END
GO

-- Returns 2 result sets: header and lines
CREATE OR ALTER PROCEDURE sp_Purchase_GetById
    @PurchaseId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT h.PurchaseId, h.SupplierId, s.SupplierName, h.InvoiceNo, h.InvoiceDate,
           h.GrossAmount, h.DiscountAmount, h.TaxAmount, h.NetAmount,
           h.Status, h.CreatedOn, h.PostedOn
    FROM PurchaseHeader h
    INNER JOIN Supplier s ON s.SupplierId = h.SupplierId
    WHERE h.PurchaseId = @PurchaseId;

    SELECT d.DetailId, d.PurchaseId, d.ProductId, p.ProductName, d.LotNo, d.ExpiryDate,
           d.Quantity, d.Rate, d.DiscountPercent, d.GrossAmount, d.DiscountAmount, d.TaxableAmount,
           d.TaxId, d.TaxName, d.TaxType, d.TaxRate, d.AdditionalRate, d.TaxAmount, d.NetAmount
    FROM PurchaseDetail d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE d.PurchaseId = @PurchaseId
    ORDER BY d.DetailId;
END
GO

/*
   Save purchase as Draft (new or edit).
   1. Validate header and lines
   2. Save header, delete old lines, insert new lines with tax copied from product
   3. Calculate line amounts and header totals
   All in one transaction.
*/
CREATE OR ALTER PROCEDURE sp_Purchase_Save
    @PurchaseId  INT,
    @SupplierId  INT,
    @InvoiceNo   VARCHAR(50),
    @InvoiceDate DATE,
    @Details     PurchaseDetailType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @msg VARCHAR(500);
    DECLARE @row INT;
    DECLARE @lot VARCHAR(50);
    DECLARE @productName VARCHAR(150);
    DECLARE @today DATE = CAST(GETDATE() AS DATE);

    ---------- header validation ----------
    IF @PurchaseId > 0 AND EXISTS (SELECT 1 FROM PurchaseHeader WHERE PurchaseId = @PurchaseId AND Status = 'Posted')
        THROW 50001, 'Posted purchase cannot be changed', 1;

    IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierId = @SupplierId AND IsActive = 1)
        THROW 50001, 'Select an active supplier', 1;

    IF EXISTS (SELECT 1 FROM PurchaseHeader WHERE SupplierId = @SupplierId AND InvoiceNo = @InvoiceNo AND PurchaseId <> @PurchaseId)
        THROW 50001, 'This invoice number is already entered for this supplier', 1;

    IF @InvoiceDate > @today
        THROW 50001, 'Invoice date cannot be a future date', 1;

    IF NOT EXISTS (SELECT 1 FROM @Details)
        THROW 50001, 'Add at least one product', 1;

    ---------- line validation ----------

    -- product must exist and be active
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo
    FROM @Details d
    LEFT JOIN Product p ON p.ProductId = d.ProductId
    WHERE p.ProductId IS NULL OR p.IsActive = 0
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': product is not active';
        THROW 50001, @msg, 1;
    END

    -- lot number is mandatory
    SET @row = NULL;
    SELECT TOP 1 @row = RowNo FROM @Details WHERE TRIM(ISNULL(LotNo, '')) = '' ORDER BY RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': lot number is required';
        THROW 50001, @msg, 1;
    END

    -- quantity, rate and discount
    SET @row = NULL;
    SELECT TOP 1 @row = RowNo FROM @Details WHERE ISNULL(Quantity, 0) <= 0 ORDER BY RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': quantity must be greater than zero';
        THROW 50001, @msg, 1;
    END

    SET @row = NULL;
    SELECT TOP 1 @row = RowNo FROM @Details WHERE ISNULL(Rate, 0) < 0 ORDER BY RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': rate cannot be negative';
        THROW 50001, @msg, 1;
    END

    SET @row = NULL;
    SELECT TOP 1 @row = RowNo FROM @Details WHERE ISNULL(DiscountPercent, 0) NOT BETWEEN 0 AND 100 ORDER BY RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': discount % must be between 0 and 100';
        THROW 50001, @msg, 1;
    END

    -- same product + lot entered twice in this bill
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo
    FROM @Details d
    WHERE EXISTS (SELECT 1 FROM @Details d2
                  WHERE d2.ProductId = d.ProductId
                    AND TRIM(d2.LotNo) = TRIM(d.LotNo)
                    AND d2.RowNo < d.RowNo)
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': same product and lot entered twice';
        THROW 50001, @msg, 1;
    END

    -- same product + lot already purchased before (another purchase or stock)
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo, @lot = TRIM(d.LotNo), @productName = p.ProductName
    FROM @Details d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE EXISTS (SELECT 1 FROM PurchaseDetail pd
                  WHERE pd.ProductId = d.ProductId AND pd.LotNo = TRIM(d.LotNo) AND pd.PurchaseId <> @PurchaseId)
       OR EXISTS (SELECT 1 FROM Stock s
                  WHERE s.ProductId = d.ProductId AND s.LotNo = TRIM(d.LotNo))
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': lot ' + @lot + ' is already purchased for ' + @productName + '. Use a different lot';
        THROW 50001, @msg, 1;
    END

    -- expiry date required for products with expiry tracking
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo
    FROM @Details d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE p.HasExpiry = 1 AND d.ExpiryDate IS NULL
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': expiry date is required';
        THROW 50001, @msg, 1;
    END

    -- expiry date must be after invoice date
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo
    FROM @Details d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE p.HasExpiry = 1 AND d.ExpiryDate <= @InvoiceDate
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': expiry date must be after invoice date';
        THROW 50001, @msg, 1;
    END

    -- already expired products are not accepted
    SET @row = NULL;
    SELECT TOP 1 @row = d.RowNo
    FROM @Details d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE p.HasExpiry = 1 AND d.ExpiryDate < @today
    ORDER BY d.RowNo;
    IF @row IS NOT NULL
    BEGIN
        SET @msg = 'Row ' + CAST(@row AS VARCHAR) + ': product is already expired, cannot accept';
        THROW 50001, @msg, 1;
    END

    ---------- save ----------
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @PurchaseId = 0
        BEGIN
            INSERT INTO PurchaseHeader (SupplierId, InvoiceNo, InvoiceDate, Status, CreatedOn)
            VALUES (@SupplierId, @InvoiceNo, @InvoiceDate, 'Draft', GETDATE());

            SET @PurchaseId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE PurchaseHeader
            SET SupplierId = @SupplierId,
                InvoiceNo = @InvoiceNo,
                InvoiceDate = @InvoiceDate
            WHERE PurchaseId = @PurchaseId;

            DELETE FROM PurchaseDetail WHERE PurchaseId = @PurchaseId;
        END

        -- insert lines and copy tax details from product's tax
        INSERT INTO PurchaseDetail (PurchaseId, ProductId, LotNo, ExpiryDate, Quantity, Rate, DiscountPercent,
                                    TaxId, TaxName, TaxType, TaxRate, AdditionalRate)
        SELECT @PurchaseId, d.ProductId, TRIM(d.LotNo),
               CASE WHEN p.HasExpiry = 1 THEN d.ExpiryDate ELSE NULL END,
               d.Quantity, d.Rate, ISNULL(d.DiscountPercent, 0),
               t.TaxId, t.TaxName, t.TaxType, ISNULL(t.TaxRate, 0), ISNULL(t.AdditionalRate, 0)
        FROM @Details d
        INNER JOIN Product p ON p.ProductId = d.ProductId
        LEFT JOIN Tax t ON t.TaxId = p.TaxId
        ORDER BY d.RowNo;

        -- line calculation, step by step
        -- Gross = Qty x Rate
        UPDATE PurchaseDetail
        SET GrossAmount = ROUND(Quantity * Rate, 2)
        WHERE PurchaseId = @PurchaseId;

        -- Discount = Gross x Disc%
        UPDATE PurchaseDetail
        SET DiscountAmount = ROUND(GrossAmount * DiscountPercent / 100, 2)
        WHERE PurchaseId = @PurchaseId;

        -- Taxable = Gross - Discount
        UPDATE PurchaseDetail
        SET TaxableAmount = GrossAmount - DiscountAmount
        WHERE PurchaseId = @PurchaseId;

        -- Single   : Tax = Taxable x Rate%
        -- Compound : Tax = Tax1 + (Taxable + Tax1) x AdditionalRate%
        UPDATE PurchaseDetail
        SET TaxAmount = ROUND(TaxableAmount * TaxRate / 100, 2)
                      + CASE WHEN TaxType = 'Compound'
                             THEN ROUND((TaxableAmount + ROUND(TaxableAmount * TaxRate / 100, 2)) * AdditionalRate / 100, 2)
                             ELSE 0
                        END
        WHERE PurchaseId = @PurchaseId;

        -- Net = Taxable + Tax
        UPDATE PurchaseDetail
        SET NetAmount = TaxableAmount + TaxAmount
        WHERE PurchaseId = @PurchaseId;

        -- header totals
        UPDATE PurchaseHeader
        SET GrossAmount    = (SELECT SUM(GrossAmount) FROM PurchaseDetail WHERE PurchaseId = @PurchaseId),
            DiscountAmount = (SELECT SUM(DiscountAmount) FROM PurchaseDetail WHERE PurchaseId = @PurchaseId),
            TaxAmount      = (SELECT SUM(TaxAmount) FROM PurchaseDetail WHERE PurchaseId = @PurchaseId),
            NetAmount      = (SELECT SUM(NetAmount) FROM PurchaseDetail WHERE PurchaseId = @PurchaseId)
        WHERE PurchaseId = @PurchaseId;

        COMMIT TRANSACTION;

        SELECT @PurchaseId AS PurchaseId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/*
   Post purchase:
   - check again (status, expiry, lot already in stock)
   - insert stock for every line
   - change status to Posted
   If anything fails, everything is rolled back. No partial stock update.
*/
CREATE OR ALTER PROCEDURE sp_Purchase_Post
    @PurchaseId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @msg VARCHAR(500);
    DECLARE @status VARCHAR(10);
    DECLARE @lot VARCHAR(50);
    DECLARE @productName VARCHAR(150);
    DECLARE @today DATE = CAST(GETDATE() AS DATE);

    SELECT @status = Status FROM PurchaseHeader WHERE PurchaseId = @PurchaseId;

    IF @status IS NULL
        THROW 50001, 'Purchase not found', 1;

    IF @status = 'Posted'
        THROW 50001, 'Purchase is already posted', 1;

    IF NOT EXISTS (SELECT 1 FROM PurchaseDetail WHERE PurchaseId = @PurchaseId)
        THROW 50001, 'Purchase has no products', 1;

    -- expired lots are not accepted into inventory
    SET @lot = NULL;
    SELECT TOP 1 @lot = d.LotNo, @productName = p.ProductName
    FROM PurchaseDetail d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    WHERE d.PurchaseId = @PurchaseId AND d.ExpiryDate < @today;
    IF @lot IS NOT NULL
    BEGIN
        SET @msg = @productName + ' lot ' + @lot + ' is expired. Cannot post.';
        THROW 50001, @msg, 1;
    END

    -- lot already in stock
    SET @lot = NULL;
    SELECT TOP 1 @lot = d.LotNo, @productName = p.ProductName
    FROM PurchaseDetail d
    INNER JOIN Product p ON p.ProductId = d.ProductId
    INNER JOIN Stock s ON s.ProductId = d.ProductId AND s.LotNo = d.LotNo
    WHERE d.PurchaseId = @PurchaseId;
    IF @lot IS NOT NULL
    BEGIN
        SET @msg = @productName + ' lot ' + @lot + ' is already in stock. Cannot post.';
        THROW 50001, @msg, 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Stock (ProductId, LotNo, ExpiryDate, Quantity, PurchaseRate, PurchaseId, ReceivedOn)
        SELECT ProductId, LotNo, ExpiryDate, Quantity, Rate, PurchaseId, GETDATE()
        FROM PurchaseDetail
        WHERE PurchaseId = @PurchaseId;

        UPDATE PurchaseHeader
        SET Status = 'Posted',
            PostedOn = GETDATE()
        WHERE PurchaseId = @PurchaseId AND Status = 'Draft';

        -- someone else posted it at the same time
        IF @@ROWCOUNT = 0
            THROW 50001, 'Purchase is already posted', 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Only draft purchase can be deleted
CREATE OR ALTER PROCEDURE sp_Purchase_Delete
    @PurchaseId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM PurchaseHeader WHERE PurchaseId = @PurchaseId AND Status = 'Posted')
        THROW 50001, 'Posted purchase cannot be deleted', 1;

    DELETE FROM PurchaseHeader WHERE PurchaseId = @PurchaseId;
END
GO

/* =========================================================
   STOCK PROCEDURES
   ========================================================= */

-- Available stock: not expired, qty > 0
-- FIFO order: earliest expiry first, then earliest received
CREATE OR ALTER PROCEDURE sp_Stock_Available
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.StockId, s.ProductId, p.ProductName, s.LotNo, s.ExpiryDate,
           s.Quantity, s.PurchaseRate, s.PurchaseId, s.ReceivedOn,
           ROW_NUMBER() OVER (PARTITION BY s.ProductId
                              ORDER BY CASE WHEN s.ExpiryDate IS NULL THEN 1 ELSE 0 END,
                                       s.ExpiryDate, s.ReceivedOn) AS FifoOrder
    FROM Stock s
    INNER JOIN Product p ON p.ProductId = s.ProductId
    WHERE s.Quantity > 0
      AND (s.ExpiryDate IS NULL OR s.ExpiryDate >= CAST(GETDATE() AS DATE))
    ORDER BY p.ProductName, FifoOrder;
END
GO

CREATE OR ALTER PROCEDURE sp_Stock_Expired
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.StockId, s.ProductId, p.ProductName, s.LotNo, s.ExpiryDate,
           s.Quantity, s.PurchaseRate, s.PurchaseId, s.ReceivedOn, 0 AS FifoOrder
    FROM Stock s
    INNER JOIN Product p ON p.ProductId = s.ProductId
    WHERE s.ExpiryDate < CAST(GETDATE() AS DATE)
    ORDER BY s.ExpiryDate;
END
GO

/* =========================================================
   DASHBOARD
   ========================================================= */

CREATE OR ALTER PROCEDURE sp_Dashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM Product WHERE IsActive = 1) AS Products,
        (SELECT COUNT(*) FROM Supplier WHERE IsActive = 1) AS Suppliers,
        (SELECT COUNT(*) FROM PurchaseHeader WHERE Status = 'Draft') AS Drafts,
        (SELECT COUNT(*) FROM PurchaseHeader WHERE Status = 'Posted') AS Posted,
        (SELECT COUNT(*) FROM Stock WHERE ExpiryDate < CAST(GETDATE() AS DATE) AND Quantity > 0) AS ExpiredLots;
END
GO

/* =========================================================
   SAMPLE DATA
   ========================================================= */

INSERT INTO Category (CategoryName) VALUES ('Medicines'), ('Grocery'), ('Cosmetics')

INSERT INTO Supplier (SupplierName, Phone, Address) VALUES
('Sri Ram Distributors', '9840012345', 'Chennai'),
('Kumar Traders', '9841198765', 'Coimbatore')

INSERT INTO Tax (TaxName, TaxType, TaxRate, AdditionalRate) VALUES
('GST 5%', 'Single', 5, 0),
('GST 12%', 'Single', 12, 0),
('GST 18% + Cess 2%', 'Compound', 18, 2)

INSERT INTO Product (ProductCode, ProductName, CategoryId, TaxId, CostPrice, SellingPrice, HasExpiry) VALUES
('P001', 'Paracetamol 500mg', 1, 2, 18.00, 25.00, 1),
('P002', 'Basmati Rice 1kg', 2, 1, 90.00, 110.00, 1),
('P003', 'Face Cream 50g', 3, 3, 120.00, 160.00, 1)
GO

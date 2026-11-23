USE LostAndFoundDB;
GO

-- 1. Users: for registration and login
CREATE TABLE Users (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    FullName      VARCHAR(100) NOT NULL,
    Email         VARCHAR(150) NOT NULL UNIQUE,
    Phone         VARCHAR(20)  NULL,
    PasswordHash  VARCHAR(255) NOT NULL,
    CreatedAt     DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

-- 2. Items: both lost and found reports
CREATE TABLE Items (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT          NOT NULL,                 -- who reported it
    ItemType     VARCHAR(10)  NOT NULL,                 -- 'Lost' or 'Found'
    Title        VARCHAR(100) NOT NULL,
    Description  VARCHAR(500) NULL,
    Category     VARCHAR(50)  NOT NULL,                 -- e.g. Phone, Wallet, Keys
    Location     VARCHAR(150) NOT NULL,
    EventDate    DATE         NOT NULL,                 -- date lost or found
    PhotoPath    VARCHAR(255) NULL,                     -- path to uploaded photo
    Status       VARCHAR(20)  NOT NULL DEFAULT 'Open',  -- 'Open' or 'Returned'
    CreatedAt    DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Items_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT CK_Items_Type   CHECK (ItemType IN ('Lost','Found')),
    CONSTRAINT CK_Items_Status CHECK (Status IN ('Open','Returned'))
);
GO

-- 3. Messages: for contacting item owners
CREATE TABLE Messages (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ItemId      INT          NOT NULL,
    SenderId    INT          NOT NULL,
    ReceiverId  INT          NOT NULL,
    Body        VARCHAR(1000) NOT NULL,
    SentAt      DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Messages_Items    FOREIGN KEY (ItemId)     REFERENCES Items(Id),
    CONSTRAINT FK_Messages_Sender   FOREIGN KEY (SenderId)   REFERENCES Users(Id),
    CONSTRAINT FK_Messages_Receiver FOREIGN KEY (ReceiverId) REFERENCES Users(Id)
);
GO
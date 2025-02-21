-- Create Devices table
CREATE TABLE Devices (
    DeviceId NVARCHAR(100) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Type NVARCHAR(50) NOT NULL,
    Location NVARCHAR(100),
    InstallationDate DATE,
    Status NVARCHAR(50),
    ProcessedAt DATETIME2 NOT NULL
);
-- Create index for better query performance
CREATE INDEX IX_Devices_Status ON Devices(Status);
CREATE INDEX IX_Devices_Type ON Devices(Type);

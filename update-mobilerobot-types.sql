-- Update MobileRobots table columns from INT to FLOAT
-- Run this in SQL Server Management Studio

USE [QES_KUKA_AMR_Penang];
GO

-- Alter columns to support decimal values
ALTER TABLE MobileRobots
    ALTER COLUMN BatteryLevel FLOAT NOT NULL;

ALTER TABLE MobileRobots
    ALTER COLUMN Mileage FLOAT NOT NULL;

ALTER TABLE MobileRobots
    ALTER COLUMN RobotOrientation FLOAT NULL;

PRINT 'MobileRobots table updated successfully!';
PRINT 'BatteryLevel, Mileage, and RobotOrientation are now FLOAT (double) type.';
GO

-- Fix: Add missing activity_status column to audit_reports table in Private Cloud Database
-- Run this SQL in your Cloud_Erase__Private database

USE Cloud_Erase__Private;

-- Check if activity_status column exists
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'Cloud_Erase__Private' 
  AND TABLE_NAME = 'audit_reports' 
  AND COLUMN_NAME = 'activity_status';

-- Add activity_status column if it doesn't exist
ALTER TABLE audit_reports 
ADD COLUMN IF NOT EXISTS activity_status VARCHAR(50) DEFAULT 'completed' 
AFTER synced;

-- Verify the column was added
DESCRIBE audit_reports;

-- Update existing records to have default status
UPDATE audit_reports 
SET activity_status = 'completed' 
WHERE activity_status IS NULL;

-- Verify update
SELECT report_id, client_email, activity_status 
FROM audit_reports 
LIMIT 5;

-- Success message
SELECT '✅ activity_status column added successfully!' AS status;

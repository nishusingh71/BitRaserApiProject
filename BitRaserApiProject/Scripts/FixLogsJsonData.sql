-- Fix invalid JSON data in the logs table
-- This script identifies and fixes invalid JSON in log_details_json column

-- First, let's identify rows with potentially invalid JSON
SELECT log_id, user_email, log_level, log_message, 
       log_details_json, 
       CASE 
         WHEN log_details_json IS NULL THEN 'NULL'
         WHEN log_details_json = '' THEN 'EMPTY'
         WHEN JSON_VALID(log_details_json) = 0 THEN 'INVALID_JSON'
         ELSE 'VALID'
       END as json_status
FROM logs 
WHERE log_details_json IS NULL 
   OR log_details_json = '' 
   OR JSON_VALID(log_details_json) = 0;

-- Fix NULL values by setting them to empty JSON object
UPDATE logs 
SET log_details_json = '{}' 
WHERE log_details_json IS NULL;

-- Fix empty strings by setting them to empty JSON object
UPDATE logs 
SET log_details_json = '{}' 
WHERE log_details_json = '';

-- For any remaining invalid JSON, set to empty JSON object
-- (Note: You might want to backup invalid data first if it contains useful information)
UPDATE logs 
SET log_details_json = '{}' 
WHERE JSON_VALID(log_details_json) = 0;

-- Verify the fix
SELECT COUNT(*) as total_logs,
       SUM(CASE WHEN log_details_json IS NULL THEN 1 ELSE 0 END) as null_count,
       SUM(CASE WHEN log_details_json = '' THEN 1 ELSE 0 END) as empty_count,
       SUM(CASE WHEN JSON_VALID(log_details_json) = 0 THEN 1 ELSE 0 END) as invalid_json_count,
       SUM(CASE WHEN JSON_VALID(log_details_json) = 1 THEN 1 ELSE 0 END) as valid_json_count
FROM logs;
-- ✅ MIGRATE MACHINES DATA TO PRIVATE DATABASE
-- Source: tech.machines
-- Target: Cloud_Erase.machines
-- User: devste@gmail.com

USE Cloud_Erase;

-- Step 1: Show migration plan
SELECT 
    '🔍 MIGRATION PLAN - MACHINES TABLE' as INFO,
    'Source: tech.machines' as SOURCE_DB,
    'Target: Cloud_Erase.machines' as TARGET_DB,
    'User: devste@gmail.com' as TARGET_USER;

-- Step 2: Count machines in source database
SELECT 
    '📊 SOURCE DATABASE ANALYSIS' as INFO,
    COUNT(*) as TOTAL_MACHINES,
  COUNT(CASE WHEN user_email = 'devste@gmail.com' THEN 1 END) as USER_MACHINES,
    COUNT(CASE WHEN subuser_email IN (
        SELECT subuser_email FROM tech.subuser WHERE user_email = 'devste@gmail.com'
    ) THEN 1 END) as SUBUSER_MACHINES
FROM tech.machines;

-- Step 3: Show sample data before migration
SELECT 
    '📋 SAMPLE MACHINES TO MIGRATE' as INFO;

SELECT 
    fingerprint_hash,
  mac_address,
    user_email,
    subuser_email,
    os_version,
    license_activated,
    created_at
FROM tech.machines
WHERE user_email = 'devste@gmail.com'
   OR subuser_email IN (
        SELECT subuser_email FROM tech.subuser WHERE user_email = 'devste@gmail.com'
    )
LIMIT 5;

-- Step 4: Migrate machines for user devste@gmail.com
-- Includes both direct machines and subuser machines
INSERT INTO Cloud_Erase.machines (
    fingerprint_hash,
    mac_address,
    physical_drive_id,
    cpu_id,
    bios_serial,
    os_version,
    user_email,
    subuser_email,
    license_activated,
    license_activation_date,
    license_days_valid,
    license_details_json,
    machine_details_json,
    vm_status,
    demo_usage_count,
    created_at,
    updated_at
)
SELECT 
    m.fingerprint_hash,
    m.mac_address,
    m.physical_drive_id,
    m.cpu_id,
    m.bios_serial,
    m.os_version,
    m.user_email,
    m.subuser_email,
  m.license_activated,
    m.license_activation_date,
    m.license_days_valid,
  m.license_details_json,
    m.machine_details_json,
    m.vm_status,
    m.demo_usage_count,
    m.created_at,
    m.updated_at
FROM tech.machines m
LEFT JOIN tech.subuser s ON m.subuser_email = s.subuser_email
WHERE m.user_email = 'devste@gmail.com'
   OR s.user_email = 'devste@gmail.com'
ON DUPLICATE KEY UPDATE
    mac_address = VALUES(mac_address),
    physical_drive_id = VALUES(physical_drive_id),
    cpu_id = VALUES(cpu_id),
    bios_serial = VALUES(bios_serial),
    os_version = VALUES(os_version),
    license_activated = VALUES(license_activated),
    license_activation_date = VALUES(license_activation_date),
    license_days_valid = VALUES(license_days_valid),
    license_details_json = VALUES(license_details_json),
    machine_details_json = VALUES(machine_details_json),
    vm_status = VALUES(vm_status),
    demo_usage_count = VALUES(demo_usage_count),
    updated_at = CURRENT_TIMESTAMP;

-- Step 5: Verify migration
SELECT 
    '✅ MIGRATION COMPLETED' as STATUS;

SELECT 
    '📊 TARGET DATABASE VERIFICATION' as INFO,
    COUNT(*) as TOTAL_MACHINES_MIGRATED,
    COUNT(CASE WHEN license_activated = 1 THEN 1 END) as LICENSED_MACHINES,
COUNT(CASE WHEN subuser_email IS NOT NULL THEN 1 END) as SUBUSER_MACHINES,
  MIN(created_at) as OLDEST_MACHINE,
    MAX(created_at) as NEWEST_MACHINE
FROM Cloud_Erase.machines
WHERE user_email = 'devste@gmail.com'
   OR subuser_email IN (
        SELECT subuser_email FROM Cloud_Erase.subuser WHERE user_email = 'devste@gmail.com'
    );

-- Step 6: Show sample migrated data
SELECT 
    '📋 SAMPLE MIGRATED MACHINES' as INFO;

SELECT 
    fingerprint_hash,
    mac_address,
    user_email,
    subuser_email,
    os_version,
  license_activated,
    CASE 
        WHEN license_activated = 1 THEN 'Licensed'
        ELSE 'Unlicensed'
    END as LICENSE_STATUS,
    created_at
FROM Cloud_Erase.machines
WHERE user_email = 'devste@gmail.com'
   OR subuser_email IN (
      SELECT subuser_email FROM Cloud_Erase.subuser WHERE user_email = 'devste@gmail.com'
    )
ORDER BY created_at DESC
LIMIT 10;

-- Step 7: Compare counts
SELECT 
    '📈 MIGRATION SUMMARY' as INFO;

SELECT 
    'SOURCE (tech.machines)' as DATABASE_NAME,
    COUNT(*) as MACHINE_COUNT
FROM tech.machines
WHERE user_email = 'devste@gmail.com'
   OR subuser_email IN (
        SELECT subuser_email FROM tech.subuser WHERE user_email = 'devste@gmail.com'
    )

UNION ALL

SELECT 
    'TARGET (Cloud_Erase.machines)' as DATABASE_NAME,
    COUNT(*) as MACHINE_COUNT
FROM Cloud_Erase.machines
WHERE user_email = 'devste@gmail.com'
   OR subuser_email IN (
    SELECT subuser_email FROM Cloud_Erase.subuser WHERE user_email = 'devste@gmail.com'
    );

-- Step 8: Show license statistics
SELECT 
    '📊 LICENSE STATISTICS' as INFO;

SELECT 
    CASE 
   WHEN license_activated = 1 THEN 'Licensed'
        ELSE 'Unlicensed'
    END as STATUS,
    COUNT(*) as COUNT,
    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Cloud_Erase.machines), 2) as PERCENTAGE
FROM Cloud_Erase.machines
GROUP BY license_activated;

SELECT 
    '🎉 Machines migration completed successfully!' as FINAL_STATUS,
    'All machines for devste@gmail.com have been migrated to Cloud_Erase database' as MESSAGE;

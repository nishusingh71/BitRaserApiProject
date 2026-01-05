-- Check if user has private cloud access enabled
SELECT 
    user_id, 
    user_email, 
    user_name,
    is_private_cloud,
    private_api,
    created_at,
  updated_at
FROM users 
WHERE user_email = 'devste@gmail.com';

-- If is_private_cloud is NULL or FALSE, run this to enable it:
-- UPDATE users SET is_private_cloud = TRUE WHERE user_email = 'devste@gmail.com';

-- Verify the update:
-- SELECT user_email, is_private_cloud FROM users WHERE user_email = 'devste@gmail.com';

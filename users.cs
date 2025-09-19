public class users
{
    public int user_id { get; set; }
    public string user_name { get; set; }
    public string user_email { get; set; }
    public string user_password { get; set; }
    public string phone_number { get; set; }
    public string payment_details_json { get; set; }
    public string license_details_json { get; set; }
    // Add this property to fix CS1061
    public string user_password_encrypted { get; set; }
}
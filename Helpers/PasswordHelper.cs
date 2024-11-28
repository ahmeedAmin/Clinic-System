using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Linq; // لاستخدام SequenceEqual للمقارنة بين المصفوفات

public static class PasswordHelper
{
    // دالة لتوليد الهاش من كلمة المرور
    public static string HashPassword(string password)
    {
        byte[] salt = new byte[16]; // توليد ملح عشوائي
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt); // ملأ الملح بالعشوائية
        }

        // توليد الهاش باستخدام PBKDF2
        byte[] hashedBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        // تحويل الملح والهاش إلى Base64 وتخزينهما معًا
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashedBytes)}";
    }

    // دالة للتحقق من كلمة المرور
    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':'); // تقسيم الملح والهاش من النص المخزن
        if (parts.Length != 2) return false; // إذا كان الشكل غير صحيح، نرجع false

        // تحويل الملح والهاش المخزن من Base64 إلى مصفوفات بايت
        var salt = Convert.FromBase64String(parts[0]);
        var storedHashBytes = Convert.FromBase64String(parts[1]);

        // توليد الهاش باستخدام كلمة المرور المدخلة والملح المخزن
        byte[] computedHashBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        // مقارنة المصفوفات (الهاش الناتج مع المخزن)
        return storedHashBytes.SequenceEqual(computedHashBytes);
    }
}

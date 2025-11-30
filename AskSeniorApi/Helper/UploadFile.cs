namespace AskSeniorApi.Helper;

public static class UploadFile
{
    public static async Task<string> UploadFileAsync(
    IFormFile file,
    string bucket,
    Supabase.Client supabase)
    {
        if (file == null || file.Length == 0)
            return null;

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type.");

        long maxSizeInBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeInBytes)
            throw new InvalidOperationException("File is too large.");

       
        var originalName = Path.GetFileName(file.FileName);


        var sanitizedFileName = originalName.Replace(" ", "_");

    
        var imagePath = $"{Guid.NewGuid()}_{sanitizedFileName}";
      

        byte[] bytes;

        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        await supabase.Storage
            .From(bucket)
            .Upload(
                bytes,                 // 1️⃣ file content
                imagePath,             // 2️⃣ filename in bucket
                new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                }
            );

        return supabase.Storage
            .From(bucket)
            .GetPublicUrl(imagePath);
    }

}

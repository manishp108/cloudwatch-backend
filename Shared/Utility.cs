namespace BackEnd.Shared
{
    public static class Utility
    {
        public static string GetFileNameFromUrl(string url)          // Extracts the file name from a given URL
        {
            // Create a Uri object from the URL
            Uri uri = new Uri(url);

            // Get the last part of the path (file name)
            string fileName = System.IO.Path.GetFileName(uri.LocalPath);

            return fileName;
        }

        
    }
}

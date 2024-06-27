using System.Text.Json;
using System.Text;
using Newtonsoft.Json;
using System.Drawing;


namespace openipc_repository_analytics
{
    internal class Program
    {
        /// <summary>
        /// Todo list:
        /// - Try to remove token
        /// - Add more variants MD markup
        /// - Review code for non-default avatars checks
        /// </summary>


        private static string accessToken = ""; // grab your token here https://github.com/settings/tokens?type=beta
        static async Task Main(string[] args)
        {
            string orgName = "openipc"; // change to your org name

            
            List<string> repositories = await GetRepositoriesAsync(orgName);

            
            List<Contributor> contributorsList = new List<Contributor>();

            
            foreach (var repo in repositories)
            {
                List<Contributor> contributors = await GetContributorsAsync(orgName, repo);
                contributorsList.AddRange(contributors);
            }

            List<Contributor> uniqueContributors = contributorsList.GroupBy(u => u.html_url).Select(g => g.First()).ToList();


            string markdownContent = GenerateMarkdown(uniqueContributors);

            string filePath = AppDomain.CurrentDomain.BaseDirectory + "/contributors.md";
            File.WriteAllText(filePath, markdownContent);

            // hardcode filter
            List<Contributor> uniqueWithFaces = new List<Contributor>();
            foreach (var contributor in uniqueContributors)
            {
                var size = await GetImageSizeFromUrl(contributor.avatar_url);
                Console.WriteLine($"name: {contributor.html_url} size: {size.Width}");

                if (size.Width != 420) // hardcode for default avatar from git
                {
                    uniqueWithFaces.Add(contributor);
                }
            }

            string markdownFacesContent = GenerateMarkdown(uniqueWithFaces);

            string filePathForOnlyWithFaces = AppDomain.CurrentDomain.BaseDirectory + "/contributors-faces.md";
            File.WriteAllText(filePathForOnlyWithFaces, markdownFacesContent);

            Console.WriteLine($"Markdown file created successfully: {filePath}");
            static async Task<List<string>> GetRepositoriesAsync(string orgName)
            {
                string apiUrl = $"https://api.github.com/users/{orgName}/repos?per_page=1000";

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("User-Agent", "C# App1");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", accessToken);

                    var request = await client.GetAsync(apiUrl);

                    var repos = await request.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<List<Repository>>(repos);
                    return response.Where(repo => repo.Fork == false).Select(repo => repo.Name).ToList();

                }
            }
            /// This method gets all contributors from your orgname & repos.

            static async Task<List<Contributor>> GetContributorsAsync(string orgName, string repoName)
            {
                string apiUrl = $"https://api.github.com/repos/{orgName}/{repoName}/contributors?per_page=1000";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", accessToken);


                    var request = await client.GetAsync(apiUrl);
                    var response = await request.Content.ReadAsStringAsync();
                    var contributors = JsonConvert.DeserializeObject<List<Contributor>>(response);

                    return contributors.ToList();

                }
            }

            static string GenerateMarkdown(List<Contributor> contributors)
            {
                var markdownBuilder = new StringBuilder();

                markdownBuilder.AppendLine("# Contributors");
                markdownBuilder.AppendLine();

                foreach (var contributor in contributors)
                {
                    markdownBuilder.AppendLine($"[<img src=\"{contributor.avatar_url}\" width=\"40px;\"/>]({contributor.html_url})");
                }
                markdownBuilder.AppendLine("### All contributors - " + contributors.Count);

                return markdownBuilder.ToString();
            }

            static async Task<Size> GetImageSizeFromUrl(string url)
            {
                var imageData = await new HttpClient().GetByteArrayAsync(url);
                using (var imgStream = new MemoryStream(imageData))
                {
                    using (var img = Image.FromStream(imgStream))
                    {
                        return new Size(img.Width, img.Height);
                    }
                }
            }
        }

        public class Repository
        {
            public bool Fork { get; set; }
            public string Name { get; set; }
        }

        public class Contributor
        {
            public string html_url { get; set; }
            public string avatar_url { get; set; }
        }
        public class ContributorAvatars
        {
            public Contributor Contributor { get; set; }
            public int Width { get; set; }
        }
    }
}

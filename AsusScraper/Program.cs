#pragma warning disable SYSLIB0014 // Type or member is obsolete
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace AsusScraper
{
    public class Product
    {
        public string? PDId { get; set; }
        public string? PDHashedId { get; set; }
        public string? PDName { get; set; }
    }

    class AsusScraper
    {
        private static string moboListUrl = "https://www.asus.com/support/api/product.asmx/GetPDLevel?website=global&type=2&typeid=1156,0&productflag=1";
        private static string moboBaseUrl = "https://www.asus.com/support/api/product.asmx/GetPDBIOS?website=korea&pdhashedid=";
        private static double query_tolerance = 0.9;
        private static double string_tolerance = 0.6;
        static void Main()
        {
            string hash = searchMobo();
            string urlString = getDownloadLink(hash);
            Console.WriteLine();

            int startInt = 0;
            int endInt = 0;
            Console.Write("Start Number: ");
            startInt = Convert.ToInt32(Console.ReadLine());
            Console.Write("End Number: ");
            endInt = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Configured Range: " + startInt.ToString("D4") + " ~ " + endInt.ToString("D4"));
            Console.WriteLine();

            int currentInt = startInt;

            while (currentInt < endInt)
            {
                string testURI = urlString + currentInt.ToString("D4") + ".ZIP";
                try
                {
                    if (!string.IsNullOrEmpty(testURI))
                    {
                        UriBuilder uriBuilder = new UriBuilder(testURI);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        Console.WriteLine();
                        Console.WriteLine();

                        Console.CursorTop -= 2;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine(testURI + " - Good                        ");
                        }
                    }
                }
                catch (Exception)
                {
                    //Console.Write("\rException at " + currentInt.ToString() + " - " + ex.Message);
                }

                Console.Write("\rCurrently Scanning " + testURI);
                currentInt++;
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Finished Processing.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static string searchMobo()
        {
            UriBuilder uriBuilder = new UriBuilder(moboListUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                dynamic jsonArray = JObject.Parse(responseText);
                List<Product> productArray = jsonArray.Result.Product.ToObject<List<Product>>();

                while(true)
                {
                    Console.Write("Motherboard Name: ");
                    string? moboName = Console.ReadLine();

                    try
                    {
                        Product selectedMobo = productArray.First(product => compareString(product.PDName, moboName) > query_tolerance);
                        string? selectedMoboHash = selectedMobo.PDHashedId;

                        if (selectedMoboHash == null)
                        {
                            Console.WriteLine("Product was found, but hash was not detected.\n");
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Product Found: " + selectedMobo.PDName);
                            return selectedMoboHash;
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Product was not found. Please search again.");
                        continue;
                    }
                }
            }
        }

        static string getDownloadLink(string hashedID)
        {
            UriBuilder uriBuilder = new UriBuilder(moboBaseUrl + hashedID);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                dynamic jsonArray = JObject.Parse(responseText);
                string downloadLink = jsonArray.Result.Obj[0].Files[0].DownloadUrl.Global;
                string filteredLink = Regex.Replace(downloadLink, "(?i)[0-9]{4}.ZIP", "");
                Console.WriteLine("Configured for Link: " + filteredLink + "0000.ZIP");
                return filteredLink;
            }
        }

        static double compareString(string source, string target)
        {
            List<string> source_exploded = source.Replace('-', ' ').Replace('_', ' ').Split(' ').ToList();
            List<string> target_exploded = target.Replace('-', ' ').Replace('_', ' ').Split(' ').ToList();

            double score = 0.0;
            int source_length = source_exploded.Count();
            int target_length = target_exploded.Count();
            int matching_words = 0;

            foreach (string source_string in source_exploded)
            {
                foreach (string target_string in target_exploded)
                {
                    if (source_string.ToUpper() == target_string.ToUpper())
                    {
                        matching_words++;
                    }
                }
            }

            score = (double)matching_words / (double)target_length;
            return score;
        }
    }
}
#pragma warning disable SYSLIB0014 // Type or member is obsolete
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

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
        static void Main()
        {
            bool retrySearch = true;
            string hash;
            List<string> urlStrings = new();

            while (retrySearch)
            {
                hash = searchMobo();
                urlStrings = getDownloadLink(hash);
                Console.Write("Is the selection correct? (y/N): ");
                retrySearch = Console.ReadLine().ToLower() != "y";
                Console.WriteLine();
            }

            int startInt = 0;
            int endInt = 0;
            Console.Write("Start Number: ");
            startInt = Convert.ToInt32(Console.ReadLine());
            Console.Write("End Number: ");
            endInt = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Configured Range: " + startInt.ToString("D4") + " ~ " + endInt.ToString("D4"));
            Console.WriteLine();
            
            Parallel.For(0, urlStrings.Count(), (i, state) =>
            {
                int currentInt = startInt;
                string urlString = urlStrings[i];
                while (currentInt < endInt + 1)
                {
                    string testURI = urlString + currentInt.ToString("D4") + ".ZIP";
                    string testURI_lw = urlString + currentInt.ToString("D4") + ".zip";
                    Console.Write("\rScanning link number " + currentInt.ToString("D4") + "     ");

                    try
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
                    catch (Exception)
                    {
                        //Console.Write("\rException at " + currentInt.ToString() + " - " + ex.Message);
                    }

                    try
                    {
                        UriBuilder uriBuilder_lw = new UriBuilder(testURI_lw);
                        HttpWebRequest request_lw = (HttpWebRequest)WebRequest.Create(uriBuilder_lw.Uri);
                        HttpWebResponse response_lw = (HttpWebResponse)request_lw.GetResponse();

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.CursorTop -= 2;

                        if (response_lw.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine(testURI_lw + " - Good                        ");
                        }
                    }
                    catch (Exception)
                    {
                        //Console.Write("\rException at " + currentInt.ToString() + " - " + ex.Message);
                    }

                    currentInt++;
                }
            });

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
                        Product selectedMobo = productArray.OrderByDescending(product => compareString(product.PDName, moboName)).First();
                        string? selectedMoboHash = selectedMobo.PDHashedId;

                        if (selectedMoboHash == null)
                        {
                            Console.WriteLine("Product was found, but hash was not detected.");
                            Console.WriteLine();
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
                        Console.WriteLine();
                        continue;
                    }
                }
            }
        }

        static List<string> getDownloadLink(string hashedID)
        {
            UriBuilder uriBuilder = new UriBuilder(moboBaseUrl + hashedID);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                dynamic jsonArray = JObject.Parse(responseText);
                dynamic fileArray = jsonArray.Result.Obj[0].Files;
                List<string> returnArray = new List<string>();

                Console.WriteLine("Configured for Links: ");
                foreach (dynamic fileObj in fileArray)
                {
                    string fileLink = fileObj.DownloadUrl.Global;
                    string filteredLink = Regex.Replace(fileLink, "(?i)[0-9]{4}.ZIP", "");
                    if (!returnArray.Contains(filteredLink))
                    {
                        returnArray.Add(filteredLink);
                        Console.WriteLine("\t" + filteredLink + "0000.ZIP");
                    }
                }

                Console.WriteLine();
                return returnArray;
            }
        }

        static double compareString(string source, string target)
        {
            List<string> source_exploded = source.Replace("-", "").Replace("_", "").Split(' ').ToList();
            List<string> target_exploded = target.Replace("-", "").Replace("_", "").Split(' ').ToList();

            double score = 0.0;
            int source_length = source_exploded.Count();
            int target_length = target_exploded.Count();
            int matching_words = 0;
            int partial_matching_words = 0;

            foreach (string source_string in source_exploded)
            {
                foreach (string target_string in target_exploded)
                {
                    if (source_string.ToUpper() == target_string.ToUpper())
                    {
                        matching_words++;
                    } else if (source_string.ToUpper().Contains(target_string.ToUpper()))
                    {
                        partial_matching_words++;
                    }
                }
            }

            score = ((double)matching_words + (double)partial_matching_words) / (double)target_length;
            return score;
        }
    }
}
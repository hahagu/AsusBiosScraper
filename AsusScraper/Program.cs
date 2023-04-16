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
        static async Task Main()
        {
            bool retrySearch = true;
            string hash;
            List<string> urlStrings = new();

            while (retrySearch)
            {
                hash = SearchMotherboard().Result;
                urlStrings = GetDownloadLink(hash);
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

            LogModel model = new()
            {
                Sender = "MainThread",
                Message = $"Configured Range: {startInt:0000} ~ {endInt:0000}",
            };

            await LogController.WriteLine(model);

            async void Scanner(int i, ParallelLoopState state)
            {
                int currentInt = startInt;
                string urlString = urlStrings[i];
                while (currentInt < endInt + 1)
                {
                    string testUri = urlString + currentInt.ToString("D4") + ".ZIP";
                    string testUriLw = urlString + currentInt.ToString("D4") + ".zip";
                    Console.Write("\rScanning link number " + currentInt.ToString("D4") + "     ");

                    try
                    {
                        UriBuilder uriBuilder = new UriBuilder(testUri);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.CursorTop -= 2;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine(testUri + " - Good                        ");

                            LogModel model = new() { Sender = $"ScannerThread_{currentInt}", Message = $"{testUri} - Good" };

                            await LogController.WriteLine(model);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogModel model = new() { Sender = $"ScannerThread_{currentInt}", Message = ex.Message };

                        await LogController.WriteLine(model);
                    }

                    try
                    {
                        UriBuilder uriBuilder_lw = new UriBuilder(testUriLw);
                        HttpWebRequest request_lw = (HttpWebRequest)WebRequest.Create(uriBuilder_lw.Uri);
                        HttpWebResponse response_lw = (HttpWebResponse)request_lw.GetResponse();

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.CursorTop -= 2;

                        if (response_lw.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine(testUriLw + " - Good                        ");

                            LogModel model = new() { Sender = $"ScannerThread_{currentInt}_Lowercase", Message = $"{testUri} - Good" };

                            await LogController.WriteLine(model);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogModel model = new() { Sender = $"ScannerThread_{currentInt}_Lowercase", Message = ex.Message };

                        await LogController.WriteLine(model);
                    }

                    currentInt++;
                }
            }

            Parallel.For(0, urlStrings.Count(), Scanner);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Finished Processing.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task<string> SearchMotherboard()
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
                        Product selectedMobo = productArray.OrderByDescending(product => CompareString(product.PDName, moboName)).First();
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

                            LogModel model = new()
                            {
                                Sender = "SearchMotherboard",
                                Message = $"Product Found: {selectedMobo.PDName}, {selectedMobo.PDId}",
                            };

                            await LogController.WriteLine(model);

                            return selectedMoboHash;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Product was not found. Please search again.");
                        Console.WriteLine();
                        continue;
                    }
                }
            }
        }

        static List<string> GetDownloadLink(string hashedID)
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

        static double CompareString(string source, string target)
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
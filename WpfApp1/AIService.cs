using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AiService
{

    public async Task<string> GetAiResponse(string userMessage)
    {
        using (HttpClient client = new HttpClient())
        {
            var requestData = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "Jesteś pomocnym AI do gry w warcaby." },
                    new { role = "user", content = userMessage }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            string responseString = await response.Content.ReadAsStringAsync();

            dynamic responseObject = JsonConvert.DeserializeObject(responseString);
            return responseObject.choices[0].message.content;
        }
    }
}
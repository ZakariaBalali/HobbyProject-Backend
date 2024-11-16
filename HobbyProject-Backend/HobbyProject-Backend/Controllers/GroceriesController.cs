using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("[controller]")]
[Authorize]  // Secure the actions in this controller
public class GroceriesController : ControllerBase
{
    private readonly ILogger<GroceriesController> _logger;

    public GroceriesController(ILogger<GroceriesController> logger)
    {
        _logger = logger;
    }

    private string GetUsernameFromToken()
    {
        _logger.LogInformation("xdlmao {Usernames}", User.Identity?.Name);
        return User.Identity?.Name; // Assuming username is stored in the Name claim
    }

    private string GetUserFilePath(string username)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), $"{username}_groceries.json");
    }

    [HttpPost(Name = "PostGroceries")]
    public async Task<IActionResult> Post([FromBody] GroceryItem grocery)
    {
        var username = GetUsernameFromToken();
        _logger.LogInformation("Post request received from user: {Username}", username);

        if (grocery == null)
        {
            return BadRequest("Grocery item is required.");
        }

        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("Username is required.");
        }

        grocery.Username = username;

        if (grocery.SharedWith.Contains(username, StringComparer.OrdinalIgnoreCase))
        {
            grocery.SharedWith.Remove(username);
        }

        _logger.LogInformation("Grocery received from user: {Username}. Grocery item: {@Grocery}", username, grocery);

        try
        {
            var userFilePath = GetUserFilePath(username);

            IEnumerable<GroceryItem> existingGroceries = new List<GroceryItem>();
            if (System.IO.File.Exists(userFilePath))
            {
                var existingData = System.IO.File.ReadAllText(userFilePath);
                existingGroceries = JsonSerializer.Deserialize<IEnumerable<GroceryItem>>(existingData) ?? new List<GroceryItem>();
            }

            if (existingGroceries.Any(g => g.Name.Equals(grocery.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("Item already exists in the grocery list.");
            }

            var allGroceries = existingGroceries.Concat(new[] { grocery });

            var jsonData = JsonSerializer.Serialize(allGroceries, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(userFilePath, jsonData);

            foreach (var sharedUser in grocery.SharedWith)
            {
                var sharedUserFilePath = GetUserFilePath(sharedUser);

                if (System.IO.File.Exists(sharedUserFilePath))
                {
                    var sharedData = System.IO.File.ReadAllText(sharedUserFilePath);
                    var sharedGroceries = JsonSerializer.Deserialize<List<GroceryItem>>(sharedData) ?? new List<GroceryItem>();

                    if (!sharedGroceries.Any(g => g.Name.Equals(grocery.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        sharedGroceries.Add(grocery);
                        var sharedJsonData = JsonSerializer.Serialize(sharedGroceries, new JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(sharedUserFilePath, sharedJsonData);
                    }
                }
                else
                {
                    var sharedGroceries = new List<GroceryItem> { grocery };
                    var sharedJsonData = JsonSerializer.Serialize(sharedGroceries, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(sharedUserFilePath, sharedJsonData);
                }
            }

            _logger.LogInformation("Grocery saved to file by user: {Username}. File path: {FilePath}", username, userFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save grocery for user: {Username}.", username);
            return StatusCode(500, "Failed to save grocery.");
        }

        return Ok(grocery);
    }

    [HttpDelete(Name = "DeleteGrocery")]
    public async Task<IActionResult> Delete([FromQuery] string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest("Grocery name is required.");
        }

        var username = GetUsernameFromToken();
        _logger.LogInformation("Delete request received from user: {Username} for grocery item: {Name}", username, name);

        var userFilePath = GetUserFilePath(username);

        try
        {
            if (System.IO.File.Exists(userFilePath))
            {
                var existingData = System.IO.File.ReadAllText(userFilePath);
                var groceries = JsonSerializer.Deserialize<List<GroceryItem>>(existingData) ?? new List<GroceryItem>();

                var itemToDelete = groceries.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (itemToDelete == null)
                {
                    return NotFound($"Grocery item with name '{name}' not found for user {username}.");
                }

                groceries.Remove(itemToDelete);

                var jsonData = JsonSerializer.Serialize(groceries, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(userFilePath, jsonData);

                foreach (var sharedUser in itemToDelete.SharedWith)
                {
                    var sharedUserFilePath = GetUserFilePath(sharedUser);
                    if (System.IO.File.Exists(sharedUserFilePath))
                    {
                        var sharedData = System.IO.File.ReadAllText(sharedUserFilePath);
                        var sharedGroceries = JsonSerializer.Deserialize<List<GroceryItem>>(sharedData) ?? new List<GroceryItem>();
                        sharedGroceries.Remove(itemToDelete);

                        var sharedJsonData = JsonSerializer.Serialize(sharedGroceries, new JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(sharedUserFilePath, sharedJsonData);
                    }
                }

                _logger.LogInformation("Grocery item deleted by user: {Username}. Item: {ItemName}", username, name);

                return Ok($"Grocery item '{name}' deleted successfully for user: {username}.");
            }
            else
            {
                return NotFound("User groceries file not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete grocery item for user: {Username}.", username);
            return StatusCode(500, "Failed to delete grocery item.");
        }
    }

    [HttpGet(Name = "GetGroceries")]
    public IActionResult Get()
    {
        var username = GetUsernameFromToken();
        _logger.LogInformation("Get request received from user: {Username}", username);

        var userFilePath = GetUserFilePath(username);
        List<GroceryItem> allGroceries = new List<GroceryItem>();

        try
        {
            // Retrieve groceries for the logged-in user
            if (System.IO.File.Exists(userFilePath))
            {
                var jsonData = System.IO.File.ReadAllText(userFilePath);
                var groceries = JsonSerializer.Deserialize<List<GroceryItem>>(jsonData) ?? new List<GroceryItem>();

                if (groceries != null && groceries.Any())
                {
                    allGroceries.AddRange(groceries);
                }
            }

            // Retrieve groceries shared with the logged-in user
            var allUsers = Directory.GetFiles(Directory.GetCurrentDirectory(), "*_groceries.json"); // Assuming all grocery files are named with the username

            foreach (var file in allUsers)
            {
                var usernameFromFile = Path.GetFileNameWithoutExtension(file); // Extracting the username from the file name
                if (usernameFromFile != username) // Skip the user's own grocery list
                {
                    var sharedUserData = System.IO.File.ReadAllText(file);
                    var sharedGroceries = JsonSerializer.Deserialize<List<GroceryItem>>(sharedUserData) ?? new List<GroceryItem>();

                    // Add groceries that are shared with the logged-in user
                    foreach (var grocery in sharedGroceries)
                    {
                        if (grocery.SharedWith.Contains(username, StringComparer.OrdinalIgnoreCase))
                        {
                            allGroceries.Add(grocery);
                        }
                    }
                }
            }

            if (!allGroceries.Any())
            {
                return NotFound("No groceries found.");
            }

            _logger.LogInformation("Groceries found for user: {Username}", username);
            return Ok(allGroceries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read groceries for user: {Username}.", username);
            return StatusCode(500, "Failed to read groceries.");
        }
    }

    [HttpPost("sharegrocerylist")]
    public async Task<IActionResult> ShareGroceryList([FromBody] ShareGroceryRequest request)
    {
        var username = GetUsernameFromToken();
        _logger.LogInformation("Share grocery list request received from user: {Username}", username);

        if (request == null || request.Usernames == null || !request.Usernames.Any())
        {
            return BadRequest("Valid usernames are required to share the grocery list.");
        }

        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("Username is required.");
        }

        try
        {
            // Read the grocery list of the current user
            var filePath = GetUserFilePath(username);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Grocery list not found for user.");
            }

            var existingData = System.IO.File.ReadAllText(filePath);
            var groceries = JsonSerializer.Deserialize<List<GroceryItem>>(existingData) ?? new List<GroceryItem>();

            // Loop through each grocery item and add the shared users to the SharedWith list
            foreach (var grocery in groceries)
            {
                // Add the shared usernames to the SharedWith list, ensuring there are no duplicates
                foreach (var sharedUsername in request.Usernames)
                {
                    if (!grocery.SharedWith.Contains(sharedUsername, StringComparer.OrdinalIgnoreCase) &&
                        !string.Equals(grocery.Username, sharedUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        grocery.SharedWith.Add(sharedUsername);
                    }
                }
            }

            // Serialize and save the updated list back to the file
            var jsonData = JsonSerializer.Serialize(groceries, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, jsonData);

            _logger.LogInformation("Grocery list shared by user: {Username}. Shared with: {SharedUsernames}", username, string.Join(", ", request.Usernames));
            return Ok("Grocery list shared successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share grocery list for user: {Username}.", username);
            return StatusCode(500, "Failed to share grocery list.");
        }
    }

    public class GroceryItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Supermarket { get; set; }
        public string Username { get; set; } // Owner of the item
        public List<string> SharedWith { get; set; } = new List<string>(); // List of usernames with access
    }
    public class ShareGroceryRequest
    {
        public List<string> Usernames { get; set; } = new List<string>();  // List of usernames to share with
    }

}

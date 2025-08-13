using System;
using NUnit.Framework;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenter.Models;

namespace IdeaCenter
{
    [TestFixture]
    public class IdeaCenterTests
    {
        private RestClient client;

        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjM2Q2NjZlNy1kOWNjLTRlZjEtOGVmOC1lYTBhZGYzZGY1ZjkiLCJpYXQiOiIwOC8xMy8yMDI1IDAzOjI2OjU4IiwiVXNlcklkIjoiZWIzMmE4NDUtYzkzOC00M2FiLTkzMDMtMDhkZGI0OWRlYzE3IiwiRW1haWwiOiJEdWtvdkBleGFtLmNvbSIsIlVzZXJOYW1lIjoiRHVrb3YiLCJleHAiOjE3NTUwNzcyMTgsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.ImEE0fgeA0zng1IBLN4Fy89VaABB2SBTMFdKc70MFHc";

        private const string LoginEmail = "Dukov@exam.com";

        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var TempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = TempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
        //All tests should be implemented here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnCreatedIdea()
        {   // Arrange
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            // Act
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            // Act
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);
            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 Ok");
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);

            lastCreatedIdeaId = responsItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditExistingIdea_ShouldReturnEditedIdea() 
        {
            //Arrane
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an edited idea description.",
                Url =""

            };

            // Act
            var request = new RestRequest($"/api/Idea/Edit/", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnDeletedIdea() 
        {
            //Act
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 Ok");
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoudRequiredFields_ShouldReturnBadRequest() 
        {
            // Arrange
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = "",
            };

            // Act
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnNotFound() 
        {
            // Arrange
            string nonExistingIdeaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Edited Non-Existing-Idea",
                Description = "This is updated test idea description for a non-existing idea.",
                Url = ""
            };

            // Act
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnNotFound()
        {
            // Act
            string nonExistingIdeaId = "123";
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }
        
        [OneTimeTearDown]
        public void TearDown() 
        {
            this.client?.Dispose();
        }
    }
}
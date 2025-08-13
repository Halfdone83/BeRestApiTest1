using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrepIdeaCenter.Models;

namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private static string lastCreatedIdeaId;

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI5NDhmY2YzMS03YmY2LTQ2ZTgtYTlhNC04NTkxODFkOWRiNTgiLCJpYXQiOiIwOC8xMy8yMDI1IDE3OjI2OjUzIiwiVXNlcklkIjoiOWZlNDE4NGYtMmJiYS00YmE2LWQyYWEtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJzdmVAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6IlN2ZVRlc3QiLCJleHAiOjE3NTUxMjc2MTMsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.SDU5VoTUub8_VmGgQRcFoyhuEXH8Sa4JgucrE-90rNM";

        private static string LoginEmail = "sve@example.com";
        private static string LoginPassword = "pass123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (string.IsNullOrEmpty(StaticToken))
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
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new
            {
                Email = email,
                Password = password
            });
            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }
                else
                {
                    throw new Exception("Failed to retrieve JWT token.");
                }
            }
            else
            {
                throw new Exception($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
            
        }
        [Order(1)]
        [Test]
        public void CreateIdea_ShouldReturnCreated()
        {
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea.",
                Url = ""
            });

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void Get_All_Ideas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);
                       

            var ideas = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsNotEmpty(ideas, "The list of ideas should not be empty.");

            lastCreatedIdeaId = ideas.Last().Id.ToString();
        }
        [Order(3)]
        [Test]
        public void Edit_The_Last_Idea_Created()
        {
            var request = new RestRequest($"/api/Idea/Edit/", Method.Put);

            request.AddJsonBody(new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea.",
                Url = ""
            });
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));      
        
        }
        [Order(4)]
        [Test]
        public void Delete_The_Last_Idea_Created()
        {
            var request = new RestRequest($"/api/Idea/Delete/", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void Create_Idea_Without_Required_Fields()
        {
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(new IdeaDTO
            {
                Title = "",
                Description = ""
            });

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Order(6)]
        [Test]
        public void Edit_Non_Existing_Idea()
        {
            string nonExistingIdea = "nqmaMe";
            var request = new RestRequest($"/api/Idea/Edit/", Method.Put);

            request.AddJsonBody(new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea.",
                Url = ""
            });
            request.AddQueryParameter("ideaId", nonExistingIdea);
            var response = this.client.Execute(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        }

        [Order(7)]
        [Test]
        public void Delete_Non_Existing_Idea()
        {
            var request = new RestRequest($"/api/Idea/Delete/", Method.Delete);
            request.AddQueryParameter("ideaId", "lastCreatedIdeaId");
            var response = this.client.Execute(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            client.Dispose();
        }


    }
}
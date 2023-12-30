using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Test_Taste_Console_Application.Constants;
using Test_Taste_Console_Application.Domain.DataTransferObjects;
using Test_Taste_Console_Application.Domain.DataTransferObjects.JsonObjects;
using Test_Taste_Console_Application.Domain.Objects;
using Test_Taste_Console_Application.Domain.Services.Interfaces;
using Test_Taste_Console_Application.Utilities;

namespace Test_Taste_Console_Application.Domain.Services
{
    /// <inheritdoc />
    public class PlanetService : IPlanetService
    {
        private readonly HttpClientService _httpClientService;

        public PlanetService(HttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        public IEnumerable<Planet> GetAllPlanets()
        {
            var allPlanetsWithTheirMoons = new Collection<Planet>();

            var response = _httpClientService.Client
                .GetAsync(UriPath.GetAllPlanetsWithMoonsQueryParameters)
                .Result;

            //If the status code isn't 200-299, then the function returns an empty collection.
            if (!response.IsSuccessStatusCode)
            {
                Logger.Instance.Warn($"{LoggerMessage.GetRequestFailed}{response.StatusCode}");
                return allPlanetsWithTheirMoons;
            }

            var content = response.Content.ReadAsStringAsync().Result;

            //The JSON converter uses DTO's, that can be found in the DataTransferObjects folder, to deserialize the response content.
            var results = JsonConvert.DeserializeObject<JsonResult<PlanetDto>>(content);

            //The JSON converter can return a null object. 
            if (results == null) return allPlanetsWithTheirMoons;

            //If the planet doesn't have any moons, then it isn't added to the collection.
            foreach (var planet in results.Bodies)
            {
                if (planet.Moons != null)
                {
                    var newMoonsCollection = new Collection<MoonDto>();
                    foreach (var moon in planet.Moons)
                    {
                        var moonResponse = _httpClientService.Client
                            .GetAsync(UriPath.GetMoonByIdQueryParameters + moon.URLId)
                            .Result;
                        var moonContent = moonResponse.Content.ReadAsStringAsync().Result;
                        MoonDto moonDto = JsonConvert.DeserializeObject<MoonDto>(moonContent);
                        moonDto.Gravity = calculateGraviity(moonDto);
                        newMoonsCollection.Add(moonDto);
                    }
                    planet.Moons = newMoonsCollection;

                }
                allPlanetsWithTheirMoons.Add(new Planet(planet));
            }

            return allPlanetsWithTheirMoons;
        }

        private double calculateGraviity(MoonDto moonDto)
        {
            double gravity = 0f;
            try
            {
                if (moonDto.MassValue > 0 && moonDto.MassExponent > 0 && moonDto.MeanRadius > 0)
                {
                    double G = 6.67430e-11;

                    // Mass values
                    double massValue = moonDto.MassValue;
                    float massExponent = moonDto.MassExponent;

                    // Mean radius in meters
                    double meanRadius = moonDto.MeanRadius * 1000; // Convert from km to meters

                    // Convert mass to standard kilograms
                    double mass = massValue * Math.Pow(10, massExponent);

                    // Calculate gravitational acceleration
                    gravity = G * mass / Math.Pow(meanRadius, 2);
                }

            }
            catch (Exception ex)
            {
                gravity = 0f;
            }

            return gravity;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }
}

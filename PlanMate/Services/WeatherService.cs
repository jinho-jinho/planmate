// Services/WeatherService.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlanMate.Models;
using System.Windows.Media;

namespace PlanMate.Services
{
    public class WeatherService
    {
        private const string ApiKey = "b58cef643ad581eb695a10e6796c9fbc";
        private const string GeoUrl = "http://ip-api.com/json/";
        private readonly HttpClient _http = new HttpClient();

        public async Task<(string City, double Temp, string Icon)> GetCurrentWeatherAsync()
        {
            // IP 기반 위치 조회
            var geoRes = await _http.GetStringAsync(GeoUrl);
            using var geoDoc = JsonDocument.Parse(geoRes);
            string city = geoDoc.RootElement.GetProperty("city").GetString();
            var lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
            var lon = geoDoc.RootElement.GetProperty("lon").GetDouble();

            // 한국어 지역명 조회
            var revUrl = $"https://api.openweathermap.org/geo/1.0/reverse?lat={lat}&lon={lon}&limit=1&appid={ApiKey}";
            var revRes = await _http.GetStringAsync(revUrl);
            using var revDoc = JsonDocument.Parse(revRes);
            if (revDoc.RootElement.GetArrayLength() > 0)
            {
                var locElem = revDoc.RootElement[0];
                if (locElem.TryGetProperty("local_names", out var localNames) &&
                    localNames.TryGetProperty("ko", out var koNameProp))
                {
                    var koName = koNameProp.GetString();
                    if (!string.IsNullOrEmpty(koName))
                        city = koName;
                }
            }

            // 현재 날씨 조회
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=metric&appid={ApiKey}";
            var res = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(res);

            var temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            var icon = doc.RootElement.GetProperty("weather")[0].GetProperty("icon").GetString();
            return (city, temp, icon);
        }

        public async Task<List<ForecastItem>> Get5DayForecastAsync()
        {
            var geoRes = await _http.GetStringAsync(GeoUrl);
            using var geoDoc = JsonDocument.Parse(geoRes);
            var lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
            var lon = geoDoc.RootElement.GetProperty("lon").GetDouble();

            var url =
              $"https://api.openweathermap.org/data/2.5/forecast" +
              $"?lat={lat}&lon={lon}&units=metric&lang=kr&appid={ApiKey}";

            var res = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(res);
            var list = doc.RootElement.GetProperty("list");

            var result = new List<ForecastItem>();
            foreach (var item in list.EnumerateArray())
            {
                var dt = item.GetProperty("dt").GetInt64();
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(dt).DateTime;
                var temp = item.GetProperty("main").GetProperty("temp").GetDouble();
                var desc = item.GetProperty("weather")[0].GetProperty("description").GetString();
                var icon = item.GetProperty("weather")[0].GetProperty("icon").GetString();

                result.Add(new ForecastItem
                {
                    DateTime = dateTime,
                    Temp = temp,
                    Description = desc,
                    Icon = icon
                });
            }
            return result;
        }
    }
}
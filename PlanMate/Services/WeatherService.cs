using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PlanMate.Models;
using Windows.Devices.Geolocation;

namespace PlanMate.Services
{
    public class WeatherService
    {
        private const string ApiKey = "b58cef643ad581eb695a10e6796c9fbc";
        private const string GeoUrl = "http://ip-api.com/json/";
        private readonly HttpClient _http = new HttpClient();

        private async Task<(double lat, double lon)?> GetGeopositionAsync()
        {
            try
            {
                var access = await Geolocator.RequestAccessAsync();
                if (access == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator { DesiredAccuracy = PositionAccuracy.Default };
                    var posTask = geolocator.GetGeopositionAsync().AsTask();
                    // 10초 타임아웃
                    if (await Task.WhenAny(posTask, Task.Delay(10000)) == posTask)
                    {
                        var pos = posTask.Result;
                        return (pos.Coordinate.Point.Position.Latitude,
                                pos.Coordinate.Point.Position.Longitude);
                    }
                }
            }
            catch
            {
                // 위치 서비스 실패 시 조용히 폴백
            }
            return null;
        }

        public async Task<(string City, double Temp, string Icon)> GetCurrentWeatherAsync()
        {
            double lat, lon;
            string city = "";

            // 1) Windows 위치 서비스 시도
            var geo = await GetGeopositionAsync();
            if (geo.HasValue)
            {
                (lat, lon) = geo.Value;
            }
            else
            {
                // 2) IP 기반 위치 조회로 폴백
                var geoRes = await _http.GetStringAsync(GeoUrl);
                using var geoDoc = JsonDocument.Parse(geoRes);
                city = geoDoc.RootElement.GetProperty("city").GetString() ?? "";
                lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
                lon = geoDoc.RootElement.GetProperty("lon").GetDouble();
            }

            // 3) 한국어 지역명 역지오코딩
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

            // 4) 현재 날씨 데이터 조회
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=metric&lang=kr&appid={ApiKey}";
            var res = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(res);


            var temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            var icon = doc.RootElement.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";

            return (city, temp, icon);
        }

        public async Task<List<ForecastItem>> Get5DayForecastAsync()
        {
            // IP 기반 위치 조회
            var geoRes = await _http.GetStringAsync(GeoUrl);
            using var geoDoc = JsonDocument.Parse(geoRes);
            var lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
            var lon = geoDoc.RootElement.GetProperty("lon").GetDouble();

            // 5일 예보 API 호출
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
                var desc = item.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";
                var iconVal = item.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";

                result.Add(new ForecastItem
                {
                    DateTime = dateTime,
                    Temp = temp,
                    Description = desc,
                    Icon = iconVal
                });
            }
            return result;
        }
    }
}
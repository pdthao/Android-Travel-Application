using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AndroidApp
{
    [Activity(Label = "@string/app_name", MainLauncher = false)]
    public class MainActivity : Activity
    {
        // UI
        EditText? etLocation, etKeyword;
        Button? btnSetLocation, btnSearch, btnPrev, btnNext, btnWishlist, btnOpenSavedList;
        TextView? tvStatus, tvTitle, tvCategory, tvDistance, tvDesc;
        ImageView? imgPlace;
        FrameLayout? cardContainer;

        // data
        Dictionary<string, (double lat, double lng)> _cities = new();
        List<ActivityItem> _results = new();
        int _index = -1;

        // current location
        bool _hasLocation = false;
        string _currentCity = "";
        double _userLat = 0, _userLng = 0;

        // wishlist (PUBLIC so SavedListActivity can read it)
        public static readonly List<ActivityItem> Wishlist = new();

        // swipe up detect
        GestureDetector? _gestureDetector;

        // image cache
        ImageCache? _imageCache;
        static readonly HttpClient _http = new HttpClient();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // bind
            etLocation = FindViewById<EditText>(Resource.Id.etLocation);
            btnSetLocation = FindViewById<Button>(Resource.Id.btnSetLocation);

            etKeyword = FindViewById<EditText>(Resource.Id.etKeyword);
            btnSearch = FindViewById<Button>(Resource.Id.btnSearch);

            tvStatus = FindViewById<TextView>(Resource.Id.tvStatus);

            cardContainer = FindViewById<FrameLayout>(Resource.Id.cardContainer);
            imgPlace = FindViewById<ImageView>(Resource.Id.imgPlace);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            tvCategory = FindViewById<TextView>(Resource.Id.tvCategory);
            tvDistance = FindViewById<TextView>(Resource.Id.tvDistance);
            tvDesc = FindViewById<TextView>(Resource.Id.tvDesc);

            btnPrev = FindViewById<Button>(Resource.Id.btnPrev);
            btnNext = FindViewById<Button>(Resource.Id.btnNext);
            btnWishlist = FindViewById<Button>(Resource.Id.btnWishlist);

            // ✅ new button
            btnOpenSavedList = FindViewById<Button>(Resource.Id.btnOpenSavedList);

            _imageCache = new ImageCache(this, _http);

            // load city list once
            LoadCitiesFromRaw();

            // swipe up to save (LIKE)
            _gestureDetector = new GestureDetector(this, new SwipeUpListener(() =>
            {
                SaveCurrentToWishlist();
            }));

            if (cardContainer != null)
            {
                cardContainer.Touch += (s, e) =>
                {
                    if (_gestureDetector != null && e.Event != null)
                        _gestureDetector.OnTouchEvent(e.Event);
                    e.Handled = true;
                };
            }

            // events
            btnSetLocation!.Click += (s, e) => SetLocation();
            btnSearch!.Click += (s, e) => DoSearch();
            btnPrev!.Click += (s, e) => Prev();
            btnNext!.Click += (s, e) => Next();

            btnWishlist!.Click += (s, e) =>
            {
                SaveCurrentToWishlist();
            };

            btnOpenSavedList!.Click += (s, e) =>
            {
                StartActivity(new Intent(this, typeof(SavedListActivity)));
            };

            UpdateStatus("Ready. Type location then SET.");
        }

        // ----------------------------
        // Location
        // ----------------------------
        void SetLocation()
        {
            var city = (etLocation?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                Toast.MakeText(this, "Type a city first", ToastLength.Short).Show();
                return;
            }

            if (_cities.TryGetValue(city.ToLowerInvariant(), out var ll))
            {
                _hasLocation = true;
                _currentCity = city;
                _userLat = ll.lat;
                _userLng = ll.lng;
                UpdateStatus($"Location set: {city} ({_userLat:F4}, {_userLng:F4})");
            }
            else
            {
                _hasLocation = false;
                _currentCity = "";
                UpdateStatus($"City not found: '{city}'");
            }
        }

        // ----------------------------
        // Search
        // ----------------------------
        void DoSearch()
        {
            if (!_hasLocation)
            {
                Toast.MakeText(this, "Set location first.", ToastLength.Short).Show();
                return;
            }

            var kw = (etKeyword?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(kw))
            {
                Toast.MakeText(this, "Type a keyword", ToastLength.Short).Show();
                return;
            }

            var all = LoadActivitiesFromRaw();
            _results = all
                    .Where(x =>
                        // keyword match
                        (x.Category.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                         x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                         x.Description.Contains(kw, StringComparison.OrdinalIgnoreCase))
                        &&
                        // ✅ city match (only show results in selected city)
                        x.Location.Contains(_currentCity, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(x =>
                    {
                        x.DistanceKm = HaversineKm(_userLat, _userLng, x.Latitude, x.Longitude);
                        return x;
                    })
                    .OrderBy(x => x.DistanceKm)
                    .ToList();


            if (_results.Count == 0)
            {
                _index = -1;
                UpdateStatus($"No results for '{kw}' near '{_currentCity}'.");
                ClearCard();
                return;
            }

            _index = 0;
            UpdateStatus($"Found {_results.Count} results for '{kw}' near '{_currentCity}'.");
            _ = RenderCardAsync();
        }

        void Prev()
        {
            if (_results.Count == 0) return;
            if (_index > 0) _index--;
            _ = RenderCardAsync();
        }

        void Next()
        {
            if (_results.Count == 0) return;
            if (_index < _results.Count - 1) _index++;
            _ = RenderCardAsync();
        }

        // ----------------------------
        // Card UI
        // ----------------------------
        async Task RenderCardAsync()
        {
            if (_index < 0 || _index >= _results.Count)
            {
                ClearCard();
                return;
            }

            var item = _results[_index];


            tvTitle!.Text = item.Name;
            tvCategory!.Text = $"Category: {item.Category}, Distance: {item.DistanceKm:F1} km";
            tvDistance!.Visibility = ViewStates.Gone;
            tvDesc!.Text = item.Description;


            imgPlace!.SetImageResource(Android.Resource.Color.DarkerGray);
            await _imageCache!.LoadIntoAsync(item.ImageUrl, imgPlace);
        }

        void ClearCard()
        {
            tvTitle!.Text = "-";
            tvCategory!.Text = "Category: -";
            tvDistance!.Text = "Distance: -";
            tvDistance!.Visibility = ViewStates.Gone;
            tvDesc!.Text = "";
            imgPlace!.SetImageResource(Android.Resource.Color.DarkerGray);
        }

        // ----------------------------
        // Wishlist
        // ----------------------------
        void SaveCurrentToWishlist()
        {
            if (_index < 0 || _index >= _results.Count) return;

            var item = _results[_index];

            if (!Wishlist.Any(x => x.Name == item.Name && x.Location == item.Location))
            {
                Wishlist.Add(item);
                Toast.MakeText(this, $"Saved: {item.Name}", ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(this, "Already saved.", ToastLength.Short).Show();
            }
        }

        void ShowWishlistDialog()
        {
            if (Wishlist.Count == 0)
            {
                Toast.MakeText(this, "Wishlist is empty.", ToastLength.Short).Show();
                return;
            }

            var lines = Wishlist
                .Select(x => $"{x.Name} • {x.Category} • {x.DistanceKm:F1} km")
                .ToArray();

            new AlertDialog.Builder(this)
                .SetTitle("Saved / Wishlist")
                .SetItems(lines, (s, e) => { })
                .SetPositiveButton("OK", (s, e) => { })
                .Show();
        }

        // ----------------------------
        // CSV Loading (RAW)
        // ----------------------------
        void LoadCitiesFromRaw()
        {
            _cities.Clear();
            using var stream = Resources.OpenRawResource(Resource.Raw.cityinformation);
            using var reader = new StreamReader(stream);

            string? line;
            bool first = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (first) { first = false; continue; }
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsvLine(line);
                if (cols.Length < 4) continue;

                var city = cols[1].Trim().ToLowerInvariant();
                if (!double.TryParse(cols[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) continue;
                if (!double.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)) continue;

                _cities[city] = (lat, lng);
            }
        }

        List<ActivityItem> LoadActivitiesFromRaw()
        {
            var list = new List<ActivityItem>();

            using var stream = Resources.OpenRawResource(Resource.Raw.activities);
            using var reader = new StreamReader(stream);

            string? line;
            bool first = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (first) { first = false; continue; }
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsvLine(line);

                // OLD: 8 columns
                // NEW: 9 columns:
                // 0 Id, 1 Name, 2 Category, 3 Location, 4 Lat, 5 Lng, 6 Desc, 7 OriginalUrl, 8 ImageUrl250
                if (cols.Length < 8) continue;

                string imageUrl = "";

                if (cols.Length >= 9)
                {
                    imageUrl = cols[8].Trim();
                    if (string.IsNullOrWhiteSpace(imageUrl))
                        imageUrl = cols[7].Trim();
                }
                else
                {
                    imageUrl = cols[7].Trim();
                }

                var item = new ActivityItem
                {
                    Id = cols[0].Trim(),
                    Name = cols[1].Trim(),
                    Category = cols[2].Trim(),
                    Location = cols[3].Trim(),
                    Latitude = ParseDouble(cols[4]),
                    Longitude = ParseDouble(cols[5]),
                    Description = cols[6].Trim(),
                    ImageUrl = imageUrl,
                };

                list.Add(item);
            }

            return list;
        }

        static double ParseDouble(string s)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v;
            return 0;
        }

        static string[] SplitCsvLine(string line)
        {
            var res = new List<string>();
            bool inQuotes = false;
            var cur = "";

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];

                if (ch == '\"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    res.Add(cur);
                    cur = "";
                }
                else
                {
                    cur += ch;
                }
            }

            res.Add(cur);
            return res.ToArray();
        }

        // ----------------------------
        // Distance (Haversine)
        // ----------------------------
        static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        static double ToRad(double deg) => deg * Math.PI / 180.0;

        void UpdateStatus(string msg) => tvStatus!.Text = msg;

        public class ActivityItem
        {
            public string Id = "";
            public string Name = "";
            public string Category = "";
            public string Location = "";
            public double Latitude;
            public double Longitude;
            public string Description = "";
            public string ImageUrl = "";
            public double DistanceKm;
        }

        class SwipeUpListener : GestureDetector.SimpleOnGestureListener
        {
            readonly Action _onSwipeUp;
            const int SWIPE_MIN_DISTANCE = 120;
            const int SWIPE_THRESHOLD_VELOCITY = 200;

            public SwipeUpListener(Action onSwipeUp) => _onSwipeUp = onSwipeUp;

            public override bool OnFling(MotionEvent? e1, MotionEvent? e2, float velocityX, float velocityY)
            {
                if (e1 == null || e2 == null) return false;

                float diffY = e1.GetY() - e2.GetY();
                if (diffY > SWIPE_MIN_DISTANCE && Math.Abs(velocityY) > SWIPE_THRESHOLD_VELOCITY)
                {
                    _onSwipeUp();
                    return true;
                }
                return false;
            }
        }

        class ImageCache
        {
            readonly Activity _ctx;
            readonly HttpClient _http;
            readonly Android.Util.LruCache _mem;

            public ImageCache(Activity ctx, HttpClient http)
            {
                _ctx = ctx;
                _http = http;

                int maxMemKb = (int)(Java.Lang.Runtime.GetRuntime().MaxMemory() / 1024);
                int cacheSizeKb = maxMemKb / 8;
                _mem = new Android.Util.LruCache(cacheSizeKb);
            }

            public async Task LoadIntoAsync(string url, ImageView target)
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    target.SetImageResource(Android.Resource.Color.DarkerGray);
                    return;
                }

                var cached = _mem.Get(url) as Bitmap;
                if (cached != null)
                {
                    target.SetImageBitmap(cached);
                    return;
                }

                var fileName = "img_" + Math.Abs(url.GetHashCode()) + ".jpg";
                var filePath = System.IO.Path.Combine(_ctx.CacheDir!.AbsolutePath!, fileName);

                try
                {
                    Bitmap bmp;

                    if (File.Exists(filePath))
                    {
                        bmp = BitmapFactory.DecodeFile(filePath)!;
                    }
                    else
                    {
                        var bytes = await _http.GetByteArrayAsync(url);
                        File.WriteAllBytes(filePath, bytes);
                        bmp = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length)!;
                    }

                    if (bmp != null)
                    {
                        _mem.Put(url, bmp);
                        target.SetImageBitmap(bmp);
                    }
                    else
                    {
                        target.SetImageResource(Android.Resource.Color.DarkerGray);
                    }
                }
                catch
                {
                    target.SetImageResource(Android.Resource.Color.DarkerGray);
                }
            }
        }
    }
}

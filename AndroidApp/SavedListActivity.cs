using Android.App;
using Android.OS;
using Android.Widget;
using AndroidApp;
using System.Linq;

namespace AndroidApp
{
    [Activity(Label = "Saved List")]
    public class SavedListActivity : Activity
    {
        ListView? lvSaved;
        Button? btnBack;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_saved_list);

            lvSaved = FindViewById<ListView>(Resource.Id.lvSaved);
            btnBack = FindViewById<Button>(Resource.Id.btnBack);

            var lines = MainActivity.Wishlist
                .Select(x => $"{x.Name}\nLocation: {x.Location}, Distance: {x.DistanceKm:F1} km")
                .ToList();


            if (lines.Count == 0)
                lines.Add("No saved items yet.");

            lvSaved!.Adapter = new ArrayAdapter<string>(
                this,
                Android.Resource.Layout.SimpleListItem1,
                lines
            );

            btnBack!.Click += (s, e) => Finish();
        }
    }
}

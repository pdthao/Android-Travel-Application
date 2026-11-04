using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidApp;

namespace AndroidApp_DayHop
{
    [Activity(Label = "DayHop", Theme = "@style/Theme.AppCompat.Light.NoActionBar", MainLauncher = true)]
    public class LockActivity : Activity, View.IOnTouchListener
    {
        private View? _root;
        private float _startY;
        private const int SwipeUpThresholdPx = 180;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_lock);

            _root = FindViewById(Resource.Id.lockRoot);
            _root?.SetOnTouchListener(this);
        }

        public bool OnTouch(View? v, MotionEvent? e)
        {
            if (e == null) return false;

            if (e.Action == MotionEventActions.Down)
            {
                _startY = e.GetY();
                return true;
            }

            if (e.Action == MotionEventActions.Up)
            {
                float endY = e.GetY();
                float deltaY = _startY - endY;

                if (deltaY > SwipeUpThresholdPx)
                {
                    StartActivity(new Intent(this, typeof(MainActivity)));
                    Finish();
                }
                return true;
            }

            return false;
        }
    }
}

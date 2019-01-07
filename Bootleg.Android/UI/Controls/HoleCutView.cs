/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Preferences;
using Android.Text;

namespace Bootleg.Droid.UI
{
    public static class HelpOverlay
    {
        public enum OverlayShape { SQUARE, ROUND };

        public static HoleCutView ShowOverlay(Context context, string title, string description, bool showalways = false)
        {
            //return config:
            //ISharedPreferences preferences = PreferenceManager.GetDefaultSharedPreferences(context);

            var settings = context.GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT + "_HelpOverlay",FileCreationMode.Private);
            //settings.GetInt("IHELP_" + title, -88);
            if (!showalways)
                showalways = !settings.GetBoolean("HELP_" + title,false);

            Dialog dialog = new Dialog(context, Android.Resource.Style.ThemeBlackNoTitleBarFullScreen);
            FrameLayout v = new FrameLayout(context);
            v.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            v.SetBackgroundColor(Color.Transparent);
            var hc = new HoleCutView(context,dialog);
            hc.OnClose += () => { dialog.Cancel(); };

            hc.SetBackgroundColor(Color.Transparent);
            hc.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            v.AddView(hc);

            var inf = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            v.AddView(inf.Inflate(Resource.Layout.help_cutout, null));
            v.FindViewById<TextView>(Resource.Id.title).Text = title;
            v.FindViewById<TextView>(Resource.Id.desc).Text = description;
            v.FindViewById<Button>(Resource.Id.closebtn).Click += (o, e) => dialog.Cancel();
            dialog.SetContentView(v);
            dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (showalways)
            {
                dialog.Show();
            }

            //ISharedPreferencesEditor editor = preferences.Edit();
            var editor = settings.Edit();
            editor.PutBoolean("HELP_" + title, true);
            //editor.PutInt("IHELP_" + title, 0);
            editor.Commit();
            return hc;
        }
    }

    public class HoleCutView:FrameLayout
    {
        public HoleCutView(Context context):base(context)
        {
            SetWillNotDraw(false);
        }

        public HoleCutView(Context context,Dialog dialog) : base(context)
        {
            thedialog = dialog;
            SetWillNotDraw(false);
        }

        public void Show()
        {
            thedialog.Show();
        }

        private Dialog thedialog;

        public event Action OnClose;

        private void Button_Click(object sender, EventArgs e)
        {
            //cancle dialog
            OnClose?.Invoke();
        }

        //private Canvas temp;
        private Paint paint;
        //private Paint pt = new Paint();
        private Paint transparentPaint;

        Canvas tmp;
        Bitmap bmp;
        Paint textPaint;
        Paint blueline;

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            //temp = new Canvas(bitmap);
            //bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            paint = new Paint();
            paint.Color = Color.Argb(230,0,0,0);
            transparentPaint = new Paint();
            transparentPaint.Color = Color.Argb(0, 0, 0, 0);
            transparentPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
            transparentPaint.AntiAlias = true;

            textPaint = new Paint(PaintFlags.AntiAlias);
            textPaint.Color = Color.White;
            textPaint.TextSize = Utils.sp2px(Context, 18);
            Typeface currentTypeFace = textPaint.Typeface;
            Typeface bold = Typeface.Create(currentTypeFace, TypefaceStyle.Normal);
            textPaint.SetTypeface(bold);

            bmp = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            tmp = new Canvas(bmp);

            blueline = new Paint(PaintFlags.AntiAlias);
            blueline.StrokeWidth = 5;
            blueline.Color = Resources.GetColor(Resource.Color.dark_orange);
            blueline.SetStyle(Paint.Style.Stroke);
            tmp.DrawRect(0, 0, MeasuredWidth, MeasuredHeight, paint);
        }


        struct config
        {
            public View view;
            public Rect rec;
            public string text;
            public HelpOverlay.OverlayShape shape;
            public GravityFlags position;
        }

        List<config> points = new List<config>();

        public void AddTargetView(View v,HelpOverlay.OverlayShape shape)
        {
            points.Add(new config() {view = v, shape = shape });
            PostInvalidate();
        }

        public void AddTargetRect(Rect v, HelpOverlay.OverlayShape shape,string text,GravityFlags position)
        {
            points.Add(new config() { rec = v, shape = shape,text = text, position = position});
            PostInvalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            //canvas.DrawBitmap(bitmap, 0, 0, p);
            foreach (var p in points)
            {
                if (p.view != null)
                {
                    Rect r = new Rect();
                    p.view.GetGlobalVisibleRect(r);
                    //points.Add(new pointr() { p = new Point(r.Left, r.Top), radius = r.Width() });

                    switch (p.shape)
                    {
                        case HelpOverlay.OverlayShape.ROUND:

                            tmp.DrawCircle(r.Left + r.Width() / 2, r.Top + r.Width() / 2, r.Width(), transparentPaint);
                            tmp.DrawCircle(r.Left + r.Width() / 2, r.Top + r.Width() / 2, r.Width(), blueline);
                            break;
                        case HelpOverlay.OverlayShape.SQUARE:
                            tmp.DrawRect(r.Left - Utils.dp2px(Context, 5), r.Top - Utils.dp2px(Context, 5), r.Right + Utils.dp2px(Context, 10), r.Bottom + Utils.dp2px(Context, 10), transparentPaint);
                            tmp.DrawRect(r.Left - Utils.dp2px(Context, 5), r.Top - Utils.dp2px(Context, 5), r.Right + Utils.dp2px(Context, 10), r.Bottom + Utils.dp2px(Context, 10), blueline);
                            break;
                    }

                    if (r.Left > canvas.Width / 2)
                    {
                        textPaint.TextAlign = Paint.Align.Right;
                        tmp.DrawText(p.view.ContentDescription?.ToString() ?? p.view.Id.ToString(), r.Left - r.Width(), r.Top + r.Width() / 2, textPaint);
                    }
                    else
                    {
                        textPaint.TextAlign = Paint.Align.Right;
                        tmp.DrawText(p.view.ContentDescription?.ToString() ?? p.view.Id.ToString(), r.Right + r.Width(), r.Top + r.Width() / 2, textPaint);
                    }
                }
                else
                {
                    Rect r = p.rec;
                    switch (p.shape)
                    {
                        case HelpOverlay.OverlayShape.ROUND:
                            tmp.DrawCircle(r.Left + r.Width() / 2, r.Top + r.Width() / 2, r.Width(), transparentPaint);
                            tmp.DrawCircle(r.Left + r.Width() / 2, r.Top + r.Width() / 2, r.Width(), blueline);
                            break;
                        case HelpOverlay.OverlayShape.SQUARE:
                            tmp.DrawRect(r.Left - Utils.dp2px(Context, 5), r.Top - Utils.dp2px(Context, 5), r.Right + Utils.dp2px(Context, 10), r.Bottom + Utils.dp2px(Context, 10), transparentPaint);
                            tmp.DrawRect(r.Left - Utils.dp2px(Context, 5), r.Top - Utils.dp2px(Context, 5), r.Right + Utils.dp2px(Context, 10), r.Bottom + Utils.dp2px(Context, 10), blueline);
                            break;
                    }

                    switch(p.position)
                    {
                        case GravityFlags.Top:
                            textPaint.TextAlign = Paint.Align.Left;
                            tmp.DrawText(p.text, r.Left, r.Top - Utils.dp2px(Context, 20), textPaint);
                            break;

                        case GravityFlags.Bottom:
                            textPaint.TextAlign = Paint.Align.Left;
                            tmp.DrawText(p.text, r.Left - Utils.dp2px(Context, 20), r.Bottom + Utils.dp2px(Context, 40), textPaint);
                            break;

                        case GravityFlags.Left:
                            textPaint.TextAlign = Paint.Align.Left;
                            tmp.DrawText(p.text, r.Left - r.Width(), r.Top + r.Width() / 2, textPaint);
                            break;

                        case GravityFlags.Right:
                            TextPaint mTextPaint = new TextPaint(textPaint);
                            StaticLayout mTextLayout = new StaticLayout(p.text, mTextPaint, canvas.Width - ( r.Left + r.Width() + Utils.dp2px(Context, 15)), Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
                            // calculate x and y position where your text will be placed
                            tmp.Save();
                            tmp.Translate(r.Right + Utils.dp2px(Context, 15), r.Top + Utils.dp2px(Context, 15));
                            mTextLayout.Draw(tmp);
                            tmp.Restore();
                                                        //textPaint.TextAlign = Paint.Align.Left;
                            //tmp.DrawText(p.text, r.Right + Utils.dp2px(Context, 15), r.Top + r.Width() / 2, textPaint);
                            break;
                    }
                }
            }
            canvas.DrawBitmap(bmp,0,0,null);
        }
    }
}
/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Views;
using Android.Graphics;

namespace Bootleg.Droid.Fragments
{
    public class MySurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        Camera2Fragment fragment;
        public MySurfaceTextureListener(Camera2Fragment frag)
        {
            fragment = frag;
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface_texture, int width, int height)
        {
            fragment.openCamera(width, height,null);
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface_texture, int width, int height)
        {
            fragment.configureTransform(width, height);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface_texture)
        {
            return true;
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface_texture)
        {
        }

    }
}

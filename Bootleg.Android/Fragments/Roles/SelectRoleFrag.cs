/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidHUD;
using System.Threading;
using Bootleg.API;
using Bootleg.Droid.Adapters;
using Android.Support.V4.View;
using Android.Support.Design.Widget;
using System.Threading.Tasks;
using Bootleg.API.Exceptions;
using static Bootleg.Droid.UI.PermissionsDialog;
using Bootleg.Droid.UI;
using Bootleg.API.Model;

namespace Bootleg.Droid.Fragments
{
    public class SelectRoleFrag : Android.Support.V4.App.Fragment
    {
        public SelectRoleFrag()
        {

        }


        Shoot CurrentEvent;
        public SelectRoleFrag(Shoot ev,bool dark)
        {
            CurrentEvent = ev;
            this.dark = dark;
        }

        bool dark = false;
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RetainInstance = true;

            if (CurrentEvent != null && !dark)
            {
                if (CurrentEvent.roles.Count == 1 && WhiteLabelConfig.AUTO_SELECT_CAMERA)
                {
                    Tab_OnRoleSelected(CurrentEvent.roles.First());
                }
            }
        }

        public event Action OnRoleChanged;

        ViewPager _pager;
        RolePageAdapter _adapter;

        RoleListFragment rolelist;
        MapFragment map;

        public override void OnResume()
        {
            base.OnResume();


        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Roles, container, false);

            if (dark)
            {
                view.FindViewById<Android.Support.Design.Widget.TabLayout>(Resource.Id.tabs).Background = null;
                //view.FindViewById<TextView>(Resource.Id.selectcategory).SetTextColor(Color.White);
                //view.FindViewById<View>(Resource.Id.line).SetBackgroundColor(Color.White);
                view.FindViewById(Resource.Id.titleblack).Visibility = ViewStates.Visible;
                view.FindViewById(Resource.Id.titlewhite).Visibility = ViewStates.Gone;

                //view.FindViewById<TextView>(Resource.Id.selectcategory).SetCompoundDrawablesWithIntrinsicBounds(ContextCompat.GetDrawable(Context,Resource.Drawable.ic_info_white_24dp), null, null, null);
            }

            view.FindViewById(Resource.Id.titleblack).Visibility = ViewStates.Gone;
            view.FindViewById(Resource.Id.titlewhite).Visibility = ViewStates.Visible;

            if (CurrentEvent != null)
            {
                var _tabs = view.FindViewById<TabLayout>(Resource.Id.tabs);
                _pager = view.FindViewById<ViewPager>(Resource.Id.tabpager);
                _adapter = new RolePageAdapter(ChildFragmentManager,Context);

                _tabs.Visibility = ViewStates.Gone;
                _pager.Adapter = _adapter;

                //has a map view:
                if (CurrentEvent.roleimg != null)
                {
                    if (savedInstanceState == null)
                    {
                        map = new MapFragment(CurrentEvent, dark);
                        map.OnRoleSelected += Tab_OnRoleSelected;
                    }
                    else
                    {
                        map = FragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as MapFragment;
                    }
                    _adapter.AddTab(RolePageAdapter.TabType.MAP, map);
                    _tabs.Visibility = ViewStates.Visible;

                    if (WhiteLabelConfig.MAP_SELECTION_ONLY)
                    {
                        _tabs.Visibility = ViewStates.Gone;
                    }
                }


                //show role list when image is empty:
                if (string.IsNullOrEmpty(CurrentEvent.roleimg))
                {

                    if (CurrentEvent.phases != null && CurrentEvent.CurrentPhase.roles != null && CurrentEvent.CurrentPhase.roles.Any())
                    {
                        var phasestouse = (from n in CurrentEvent.phases where n.roles != null select n).ToList();

                        //if ther is a current phase:
                        if (phasestouse.Contains(CurrentEvent.CurrentPhase))
                        {
                            if (savedInstanceState == null)
                            {
                                rolelist = new RoleListFragment(CurrentEvent, CurrentEvent.CurrentPhase, dark);
                                rolelist.OnRoleSelected += Tab_OnRoleSelected;
                            }
                            else
                            {
                                rolelist = FragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as RoleListFragment;
                            }


                            _adapter.AddTab(RolePageAdapter.TabType.LIST, rolelist);
                            phasestouse.Remove(CurrentEvent.CurrentPhase);
                            //rolelist = rolelist;
                        }

                    }
                    else
                    {
                        if (savedInstanceState == null)
                        {
                            rolelist = new RoleListFragment(CurrentEvent, null, dark);
                            rolelist.OnRoleSelected += Tab_OnRoleSelected;
                        }
                        else
                        {
                            rolelist = FragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as RoleListFragment;
                            if (rolelist == null)
                            {
                                rolelist = new RoleListFragment(CurrentEvent, null, dark);
                                rolelist.OnRoleSelected += Tab_OnRoleSelected;
                            }
                        }
                        //var tab = new RoleListFragment(CurrentEvent, null, dark);
                        //rolelist = tab;
                        _adapter.AddTab(RolePageAdapter.TabType.LIST, rolelist);
                    }
                }

                _tabs.SetupWithViewPager(_pager);
            }
            return view;
        }

        CancellationTokenSource cancel = new CancellationTokenSource();

        bool selectingrole = false;

        private async void Tab_OnRoleSelected(Role selected)
        {
            if (!selectingrole)
            {
                selectingrole = true;
                //if its the initial connection:
                if (!dark)
                {
                    //Shoot selected = obj;
                    cancel = new CancellationTokenSource();
                    AndHUD.Shared.Show(Activity, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                    {
                        cancel.Cancel();
                        selectingrole = false;
                    });

                    try
                    {
                        cancel = new CancellationTokenSource();
                        await AskPermissions(Activity, CurrentEvent);
                        await Bootlegger.BootleggerClient.ConnectToEvent(CurrentEvent, false, cancel.Token);
                    }
                    catch (ServerErrorException)
                    {
                        AndHUD.Shared.Dismiss();

                        //show dialog:
                        new Android.Support.V7.App.AlertDialog.Builder(Activity).SetMessage(Resources.GetString(Resource.String.role_repeat))
                            .SetNegativeButton(Android.Resource.String.No, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                            {

                            }))
                            .SetPositiveButton(Android.Resource.String.Yes, new EventHandler<DialogClickEventArgs>(async (oe, eo) =>
                            {
                                AndHUD.Shared.Show(Activity, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                                {
                                    cancel.Cancel();
                                });
                                cancel = new CancellationTokenSource();
                                await Bootlegger.BootleggerClient.ConnectToEvent(CurrentEvent, true, cancel.Token);
                                await AskPermissions(Activity, CurrentEvent);
                                selectingrole = false;
                            }))
                            .SetTitle(Resource.String.areyousure)
                            .SetCancelable(false)
                            .Show();
                    }
                    catch (NotGivenPermissionException)
                    {
                        AndHUD.Shared.Dismiss();
                        selectingrole = false;
                        try
                        {
                            LoginFuncs.ShowError(this.Activity, new Exception(Resources.GetString(Resource.String.acceptperms)));
                            //Toast.MakeText(Activity, Resource.String.acceptperms, ToastLength.Long).Show();
                        }
                        catch { }
                        return;
                    }
                    catch (Exception)
                    {
                        AndHUD.Shared.Dismiss();
                        selectingrole = false;
                        try
                        {
                            //Toast.MakeText(Activity, Resource.String.problemconnecting, ToastLength.Long).Show();
                            LoginFuncs.ShowError(this.Activity, new Exception(Resources.GetString(Resource.String.problemconnecting)));
                        }
                        catch { }
                        return;
                    }
                }

                //ROLE SELECTION WHEN SHOOTING:
                AndHUD.Shared.Show(Activity, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true, () =>
                {
                    cancel.Cancel();
                    selectingrole = false;
                });
                await Task.Delay(100);
                try
                {
                    cancel = new CancellationTokenSource();
                    var res = await Bootlegger.BootleggerClient.SelectRole(selected, false, cancel.Token);
                    if (res.State == API.Model.RoleStatus.RoleState.OK)
                    {
                        OnRoleChanged?.Invoke();
                        rolelist?.Update();
                        map?.Update();
                        AndHUD.Shared.Dismiss();
                        selectingrole = false;
                    }
                    else if (res.State == API.Model.RoleStatus.RoleState.CONFIRM)
                    {
                        new Android.Support.V7.App.AlertDialog.Builder(Activity).SetMessage(res.Message)
                            .SetNegativeButton(Android.Resource.String.No, new EventHandler<DialogClickEventArgs>(async (oe, eo) =>
                            {
                                cancel = new CancellationTokenSource();
                                var ress = await Bootlegger.BootleggerClient.SelectRole(selected as Role, true, cancel.Token);
                            //listview.Enabled = true;

                            if (ress.State == API.Model.RoleStatus.RoleState.OK)
                                {
                                    OnRoleChanged?.Invoke();
                                    rolelist?.Update();
                                    map?.Update();
                                    AndHUD.Shared.Dismiss();
                                    selectingrole = false;
                                }
                                else
                                {
                                    //you are live -- so cant do this anyway
                                    AndHUD.Shared.Dismiss();
                                    selectingrole = false;
                                }
                            }))
                            .SetPositiveButton(Android.Resource.String.Yes, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                            {
                                AndHUD.Shared.Dismiss();
                                selectingrole = false;
                            }))
                            .SetTitle(Resource.String.roledescision)
                            .SetCancelable(false)
                            .Show();
                    }
                    else if (res.State == API.Model.RoleStatus.RoleState.NO)
                    {
                        //you are live and cant do it
                        AndHUD.Shared.Dismiss();
                        selectingrole = false;
                        try
                        {
                            LoginFuncs.ShowError(this.Activity, new Exception(Resources.GetString(Resource.String.cantgiverole)));

                            //Toast.MakeText(Activity, Resource.String.cantgiverole, ToastLength.Short).Show();
                        }
                        catch { }
                    }
                }
                catch (Exception e)
                {
                    AndHUD.Shared.Dismiss();
                    selectingrole = false;
                    try
                    {
                        LoginFuncs.ShowError(this.Activity, new Exception(Resources.GetString(Resource.String.problemconnecting)));
                        //Toast.MakeText(Activity, Resource.String.problemconnecting, ToastLength.Long).Show();
                    }
                    catch { }
                }
            }
        }
    }
}
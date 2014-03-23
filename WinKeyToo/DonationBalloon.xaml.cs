﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WinKeyToo.TaskbarNotification;

namespace WinKeyToo
{
    /// <summary>
    /// Interaction logic for DonationButton.xaml
    /// </summary>
    public partial class DonationBalloon
    {
    private bool isClosing;

    #region BalloonText dependency property

    /// <summary>
    /// Description
    /// </summary>
    public static readonly DependencyProperty BalloonTextProperty =
        DependencyProperty.Register("BalloonText",
                                    typeof (string),
                                    typeof(DonationBalloon),
                                    new FrameworkPropertyMetadata(""));

    /// <summary>
    /// A property wrapper for the <see cref="BalloonTextProperty"/>
    /// dependency property:<br/>
    /// Description
    /// </summary>
    public string BalloonText
    {
      get { return (string) GetValue(BalloonTextProperty); }
      set { SetValue(BalloonTextProperty, value); }
    }

    #endregion


    public DonationBalloon()
    {
      InitializeComponent();
      TaskbarIcon.AddBalloonClosingHandler(this, OnBalloonClosing);
    }


    /// <summary>
    /// By subscribing to the <see cref="TaskbarIcon.BalloonClosingEvent"/>
    /// and setting the "Handled" property to true, we suppress the popup
    /// from being closed in order to display the fade-out animation.
    /// </summary>
    private void OnBalloonClosing(object sender, RoutedEventArgs e)
    {
      e.Handled = true;
      isClosing = true;
    }


    /// <summary>
    /// Resolves the <see cref="TaskbarIcon"/> that displayed
    /// the balloon and requests a close action.
    /// </summary>
    private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
    {
      //the tray icon assigned this attached property to simplify access
      var taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
      taskbarIcon.CloseBalloon();
    }

    /// <summary>
    /// If the users hovers over the balloon, we don't close it.
    /// </summary>
    private void grid_MouseEnter(object sender, MouseEventArgs e)
    {
      //if we're already running the fade-out animation, do not interrupt anymore
      //(makes things too complicated for the sample)
      if (isClosing) return;

      //the tray icon assigned this attached property to simplify access
      var taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
      taskbarIcon.ResetBalloonCloseTimer();
    }


    /// <summary>
    /// Closes the popup once the fade-out animation completed.
    /// The animation was triggered in XAML through the attached
    /// BalloonClosing event.
    /// </summary>
    private void OnFadeOutCompleted(object sender, EventArgs e)
    {
      var pp = (Popup)Parent;
      pp.IsOpen = false;
    }

    private void imgDonate_MouseDown(object sender, MouseButtonEventArgs e)
    {
        //https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=11061744
        Process.Start("http://www.deriven.com/projects/winkeytoo/donate.html");
    }
  }
}

﻿// Copyright 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using VsChromium.Commands;
using VsChromium.Features.AutoUpdate;
using VsChromium.Wpf;

namespace VsChromium.Features.ToolWindows.SourceExplorer {
  /// <summary>
  /// This class implements the tool window exposed by this package and hosts a user control.
  ///
  /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
  /// usually implemented by the package implementer.
  ///
  /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
  /// implementation of the IVsUIElementPane interface.
  /// </summary>
  [Guid(GuidList.GuidSourceExplorerToolWindowString)]
  public class SourceExplorerToolWindow : ToolWindowPane {
    /// <summary>
    /// Standard constructor for the tool window.
    /// </summary>
    public SourceExplorerToolWindow()
      : base(null) {
      // Set the window title reading it from the resources.
      Caption = Resources.SourceExplorerToolWindowTitle;
      // Set the image that will appear on the tab of the window frame
      // when docked with an other window
      // The resource ID correspond to the one defined in the resx file
      // while the Index is the offset in the bitmap strip. Each image in
      // the strip being 16x16.
      BitmapResourceID = 301;
      BitmapIndex = 1;

      // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
      // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
      // the object returned by the Content property.
      ExplorerControl = new SourceExplorerControl();
    }

    public void FocusSearchTextBox(CommandID commandId) {
      switch (commandId.ID) {
        case PkgCmdIdList.CmdidSearchFileNames:
          ExplorerControl.FileNamesSearch.Focus();
          break;
        case PkgCmdIdList.CmdidSearchDirectoryNames:
          ExplorerControl.DirectoryNamesSearch.Focus();
          break;
        case PkgCmdIdList.CmdidSearchFileContents:
          ExplorerControl.FileContentsSearch.Focus();
          break;
      }
    }

    public SourceExplorerControl ExplorerControl {
      get { return Content as SourceExplorerControl; }
      set { Content = value; }
    }

    enum Direction {
      Next,
      Previous
    }

    private T GetNextLocationEntry<T>(Direction direction) where T:class, IHierarchyObject {
      if (ExplorerControl.ViewModel.ActiveDisplay != SourceExplorerViewModel.DisplayKind.TextSearchResult)
        return null;

      var item = ExplorerControl.FileTreeView.SelectedItem;
      if (item == null) {
        if (ExplorerControl.ViewModel.CurrentRootNodesViewModel == null)
          return null;

        if (ExplorerControl.ViewModel.CurrentRootNodesViewModel.Count == 0)
          return null;

        item = ExplorerControl.ViewModel.CurrentRootNodesViewModel[0].ParentViewModel;
        if (item == null)
          return null;
      }

      var nextItem = (direction == Direction.Next)
        ? new HierarchyObjectNavigator().GetNextItemOfType<T>(item as IHierarchyObject)
        : new HierarchyObjectNavigator().GetPreviousItemOfType<T>(item as IHierarchyObject);

      return nextItem;
    }

    public bool HasNextLocation() {
      return GetNextLocationEntry<FilePositionViewModel>(Direction.Next) != null;
    }

    public bool HasPreviousLocation() {
      return GetNextLocationEntry<FilePositionViewModel>(Direction.Previous) != null;
    }

    public void NavigateToNextLocation() {
      var nextItem = GetNextLocationEntry<FilePositionViewModel>(Direction.Next);
      if (nextItem == null)
        return;
      ExplorerControl.ViewModel.Host.SelectTreeViewItem(nextItem, () => {
        ExplorerControl.NavigateFromSelectedItem(nextItem); 
      });
    }

    public void NavigateToPreviousLocation() {
      var previousItem = GetNextLocationEntry<FilePositionViewModel>(Direction.Previous);
      if (previousItem == null)
        return;
      ExplorerControl.ViewModel.Host.SelectTreeViewItem(previousItem, () => {
        ExplorerControl.NavigateFromSelectedItem(previousItem); 
      });
    }

    public override void OnToolWindowCreated() {
      base.OnToolWindowCreated();
      ExplorerControl.OnToolWindowCreated(this);
    }

    public void NotifyPackageUpdate(UpdateInfo updateInfo) {
      WpfUtilities.Post(ExplorerControl, () => {
        ExplorerControl.UpdateInfo = updateInfo;
      });
    }
  }
}
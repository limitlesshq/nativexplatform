//-----------------------------------------------------------------------
// <copyright file="extractCLIProgram.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2017  Nicholas K. Dionysopoulos / Akeeba Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows.Forms;
using ExtractWizard.Gateway;

namespace ExtractWizard
{
    public partial class MainForm : Form
    {
        private Controller.MainForm _controller;

        public MainForm()
        {
            InitializeComponent();

            // Create the Gateway to this View
            MainFormGateway gateway = new MainFormGateway(this);
            // Create the Controller
            _controller = new Controller.MainForm(gateway);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Ask the Controller to initialize the View
            _controller.IntializeView();
        }

        private void btnBrowseArchive_Click(object sender, EventArgs e)
        {
            _controller.OnBrowseArchiveButtonClick(sender, e);
        }

        private void btnExtractToFolder_Click(object sender, EventArgs e)
        {
            _controller.OnBrowseOutputFolderButtonClick(sender, e);
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            _controller.OnDonateButtonClick(sender, e);
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            _controller.OnHelpButtonClick(sender, e);
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            _controller.OnStartStopButtonClick(sender, e);
        }
    }
}

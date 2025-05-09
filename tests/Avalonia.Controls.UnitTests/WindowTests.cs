using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WindowTests : ScopedTestBase
    {
        [Fact]
        public void Setting_Title_Should_Set_Impl_Title()
        {
            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.Setup(r => r.RenderScaling).Returns(1.0);
            windowImpl.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            var windowingPlatform = new MockWindowingPlatform(() => windowImpl.Object);

            using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
            {
                var target = new Window();

                target.Title = "Hello World";

                windowImpl.Verify(x => x.SetTitle("Hello World"));
            }
        }

        [Fact]
        public void IsVisible_Should_Initially_Be_False()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var window = new Window();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_True_After_Show()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();

                Assert.True(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_True_After_ShowDialog()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                parent.Show();
                var window = new Window();

                var task = window.ShowDialog(parent);

                Assert.True(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Hide()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();
                window.Hide();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();
                window.Close();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Impl_Signals_Close()
        {
            var windowImpl = CreateImpl();
            windowImpl.SetupProperty(x => x.Closed);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);

            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            {
                var window = new Window();

                window.Show();
                Assert.True(window.IsVisible);

                windowImpl.Object.Closed();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void Closing_Should_Only_Be_Invoked_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var count = 0;

                window.Closing +=
                    (sender, e) =>
                    {
                        count++;
                    };

                window.Show();
                window.Close();

                Assert.Equal(1, count);
            }
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Child_windows_should_be_closed_before_parent(bool programmaticClose)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var child = new Window();

                int count = 0;
                int windowClosing = 0;
                int childClosing = 0;
                int windowClosed = 0;
                int childClosed = 0;

                window.Closing += (sender, e) =>
                {
                    Assert.Equal(WindowCloseReason.WindowClosing, e.CloseReason);
                    Assert.Equal(programmaticClose, e.IsProgrammatic);
                    count++;
                    windowClosing = count;
                };
                
                child.Closing += (sender, e) =>
                {
                    Assert.Equal(WindowCloseReason.OwnerWindowClosing, e.CloseReason);
                    Assert.Equal(programmaticClose, e.IsProgrammatic);
                    count++;
                    childClosing = count;
                };
                
                window.Closed += (sender, e) =>
                {
                    count++;
                    windowClosed = count;
                };
                
                child.Closed += (sender, e) =>
                {
                    count++;
                    childClosed = count;
                };

                window.Show();
                child.Show(window);

                if (programmaticClose)
                {
                    window.Close();
                }
                else
                {
                    var cancel = window.PlatformImpl.Closing(WindowCloseReason.WindowClosing);

                    Assert.Equal(false, cancel);
                }

                Assert.Equal(2, windowClosing);
                Assert.Equal(1, childClosing);
                Assert.Equal(4, windowClosed);
                Assert.Equal(3, childClosed);
            }
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Child_windows_must_not_close_before_parent_has_chance_to_Cancel_OSCloseButton(bool programmaticClose)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var child = new Window();

                int count = 0;
                int windowClosing = 0;
                int childClosing = 0;
                int windowClosed = 0;
                int childClosed = 0;

                window.Closing += (sender, e) =>
                {
                    count++;
                    windowClosing = count;
                    e.Cancel = true;
                };
                
                child.Closing += (sender, e) =>
                {
                    count++;
                    childClosing = count;
                };
                
                window.Closed += (sender, e) =>
                {
                    count++;
                    windowClosed = count;
                };
                
                child.Closed += (sender, e) =>
                {
                    count++;
                    childClosed = count;
                };

                window.Show();
                child.Show(window);
                
                if (programmaticClose)
                {
                    window.Close();
                }
                else
                {
                    var cancel = window.PlatformImpl.Closing(WindowCloseReason.WindowClosing);

                    Assert.Equal(true, cancel);
                }

                Assert.Equal(2, windowClosing);
                Assert.Equal(1, childClosing);
                Assert.Equal(0, windowClosed);
                Assert.Equal(0, childClosed);
            }
        }

        [Fact]
        public void Showing_Should_Start_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window(CreateImpl().Object);

                target.Show();
                Assert.True(MediaContext.Instance.IsTopLevelActive(target));
            }
        }

        [Fact]
        public void ShowDialog_Should_Start_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window(CreateImpl().Object);

                parent.Show();
                target.ShowDialog<object>(parent);

                Assert.True(MediaContext.Instance.IsTopLevelActive(target));
            }
        }

        [Fact]
        public void ShowDialog_Should_Raise_Opened()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window();
                var raised = false;

                parent.Show();
                target.Opened += (s, e) => raised = true;

                target.ShowDialog<object>(parent);

                Assert.True(raised);
            }
        }

        [Fact]
        public void Hiding_Should_Stop_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window(CreateImpl().Object);

                target.Show();
                target.Hide();
                Assert.False(MediaContext.Instance.IsTopLevelActive(target));
            }
        }

        [Fact]
        public async Task ShowDialog_With_ValueType_Returns_Default_When_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var windowImpl = CreateImpl();
                windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.DesktopScaling).Returns(1);
                windowImpl.Setup(x => x.RenderScaling).Returns(1);

                parent.Show();
                var target = new Window(windowImpl.Object);
                var task = target.ShowDialog<bool>(parent);

                windowImpl.Object.Closed();

                var result = await task;
                Assert.False(result);
            }
        }

        [Fact]
        public void Calling_Show_On_Closed_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window();

                target.Show();
                target.Close();

                var openedRaised = false;
                target.Opened += (s, e) => openedRaised = true;

                var ex = Assert.Throws<InvalidOperationException>(() => target.Show());
                Assert.Equal("Cannot re-show a closed window.", ex.Message);
                Assert.False(openedRaised);
            }
        }

        [Fact]
        public async Task Calling_ShowDialog_On_Closed_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var windowImpl = CreateImpl();
                windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.DesktopScaling).Returns(1);
                windowImpl.Setup(x => x.RenderScaling).Returns(1);

                parent.Show();

                var target = new Window(windowImpl.Object);
                var task = target.ShowDialog<bool>(parent);

                windowImpl.Object.Closed();
                await task;

                var openedRaised = false;
                target.Opened += (s, e) => openedRaised = true;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog<bool>(parent));
                Assert.Equal("Cannot re-show a closed window.", ex.Message);
                Assert.False(openedRaised);
            }
        }

        [Fact]
        public void Calling_Show_With_Closed_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window();

                parent.Close();

                var ex = Assert.Throws<InvalidOperationException>(() => target.Show(parent));
                Assert.Equal("Cannot show a window with a closed owner.", ex.Message);
            }
        }

        [Fact]
        public async Task Calling_ShowDialog_With_Closed_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window();

                parent.Close();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(parent));
                Assert.Equal("Cannot show a window with a closed owner.", ex.Message);
            }
        }

        [Fact]
        public void Calling_Show_With_Invisible_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window();

                var ex = Assert.Throws<InvalidOperationException>(() => target.Show(parent));
                Assert.Equal("Cannot show window with non-visible owner.", ex.Message);
            }
        }

        [Fact]
        public async Task Calling_ShowDialog_With_Invisible_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                var target = new Window();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(parent));
                Assert.Equal("Cannot show window with non-visible owner.", ex.Message);
            }
        }

        [Fact]
        public void Calling_Show_With_Self_As_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window();

                var ex = Assert.Throws<InvalidOperationException>(() => target.Show(target));
                Assert.Equal("A Window cannot be its own owner.", ex.Message);
            }
        }

        [Fact]
        public async Task Calling_ShowDialog_With_Self_As_Parent_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog(target));
                Assert.Equal("A Window cannot be its own owner.", ex.Message);
            }
        }

        [Fact]
        public void Hiding_Parent_Window_Should_Close_Children()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var parent = new Window();
                var child = new Window();

                parent.Show();
                child.Show(parent);

                parent.Hide();

                Assert.False(parent.IsVisible);
                Assert.False(child.IsVisible);
            }
        }

        [Fact]
        public void Hiding_Parent_Window_Should_Close_Dialog_Children()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var parent = new Window();
                var child = new Window();

                parent.Show();
                child.ShowDialog(parent);

                parent.Hide();

                Assert.False(parent.IsVisible);
                Assert.False(child.IsVisible);
            }
        }

        [Fact]
        public void Window_Should_Not_Be_Centered_When_WindowStartupLocation_Is_CenterScreen_And_Window_Is_Hidden_And_Shown()
        {
            var screen1 = new Mock<Screen>(1.0, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);

            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.AllScreens).Returns(new Screen[] { screen1.Object });
            screens.Setup(x => x.ScreenFromPoint(It.IsAny<PixelPoint>())).Returns(screen1.Object);


            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window(windowImpl.Object)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                window.Show();

                var expected = new PixelPoint(150, 400);
                window.Position = expected;

                window.IsVisible = false;
                window.IsVisible = true;

                Assert.Equal(expected, window.Position);
            }
        }

        [Fact]
        public void Window_Should_Be_Centered_When_WindowStartupLocation_Is_CenterScreen()
        {
            var screen1 = new Mock<Screen>(1.0, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);
            var screen2 = new Mock<Screen>(1.0, new PixelRect(new PixelSize(1366, 768)), new PixelRect(new PixelSize(1366, 728)), false);

            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.AllScreens).Returns(new Screen[] { screen1.Object, screen2.Object });
            screens.Setup(x => x.ScreenFromPoint(It.IsAny<PixelPoint>())).Returns(screen1.Object);
            

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window(windowImpl.Object);
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Position = new PixelPoint(60, 40);

                window.Show();

                var expectedPosition = new PixelPoint(
                    (int)(screen1.Object.WorkingArea.Size.Width / 2 - window.ClientSize.Width / 2),
                    (int)(screen1.Object.WorkingArea.Size.Height / 2 - window.ClientSize.Height / 2));

                Assert.Equal(window.Position, expectedPosition);
            }
        }
        
        [Fact]
        public void Window_Should_Be_Sized_To_MinSize_If_InitialSize_Less_Than_MinSize()
        {
            var screen1 = new Mock<Screen>(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.AllScreens).Returns(new Screen[] { screen1.Object });
            screens.Setup(x => x.ScreenFromPoint(It.IsAny<PixelPoint>())).Returns(screen1.Object);
            
            var windowImpl = MockWindowingPlatform.CreateWindowMock(400, 300);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1.75);
            windowImpl.Setup(x => x.RenderScaling).Returns(1.75);
            windowImpl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);
            
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window(windowImpl.Object);
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.MinWidth = 720;
                window.MinHeight = 480;

                window.Show();
                
                Assert.Equal(new PixelPoint(330, 63), window.Position);
                Assert.Equal(new Size(720, 480), window.Bounds.Size);
            }
        }

        [Fact]
        public void Window_Should_Be_Centered_Relative_To_Owner_When_WindowStartupLocation_Is_CenterOwner()
        {
            var parentWindowImpl = MockWindowingPlatform.CreateWindowMock();
            parentWindowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            parentWindowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1920, 1080));
            parentWindowImpl.Setup(x => x.DesktopScaling).Returns(1);
            parentWindowImpl.Setup(x => x.RenderScaling).Returns(1);

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(320, 200));
            windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1920, 1080));
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);

            var parentWindowServices = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => parentWindowImpl.Object));

            var windowServices = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(parentWindowServices))
            {
                var parentWindow = new Window();
                parentWindow.Position = new PixelPoint(60, 40);

                parentWindow.Show();

                using (UnitTestApplication.Start(windowServices))
                {
                    var window = new Window();
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Position = new PixelPoint(60, 40);                    

                    window.ShowDialog(parentWindow);

                    var expectedPosition = new PixelPoint(
                        (int)(parentWindow.Position.X + parentWindow.ClientSize.Width / 2 - window.ClientSize.Width / 2),
                        (int)(parentWindow.Position.Y + parentWindow.ClientSize.Height / 2 - window.ClientSize.Height / 2));

                    Assert.Equal(window.Position, expectedPosition);
                }
            }
        }

        [Fact]
        public void Window_Topmost_By_Default_Should_Configure_PlatformImpl_When_Constructed()
        {
            var windowImpl = MockWindowingPlatform.CreateWindowMock();

            var windowServices = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(windowServices))
            {
                var window = new TopmostWindow();

                Assert.True(window.Topmost);
                windowImpl.Verify(i => i.SetTopmost(true));
            }
        }

        public class SizingTests : ScopedTestBase
        {
            [Fact]
            public void Child_Should_Be_Measured_With_Width_And_Height_If_SizeToContent_Is_Manual()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new ChildControl();
                    var target = new Window
                    {
                        Width = 100,
                        Height = 50,
                        SizeToContent = SizeToContent.Manual,
                        Content = child
                    };

                    // Verify that the child is initially measured with our Width/Height.
                    Show(target);

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(100, 50), child.MeasureSizes[0]);

                    // Now change the bounds: verify that we are using the new Width/Height, and not the old ClientSize.
                    child.MeasureSizes.Clear();
                    child.InvalidateMeasure();

                    target.Width = 120;
                    target.Height = 70;

                    Dispatcher.UIThread.RunJobs();

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(120, 70), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Child_Should_Be_Measured_With_ClientSize_If_SizeToContent_Is_Manual_And_No_Width_Height_Specified()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    windowImpl.Setup(x => x.ClientSize).Returns(new Size(550, 450));

                    var child = new ChildControl();
                    var target = new Window(windowImpl.Object)
                    {
                        SizeToContent = SizeToContent.Manual,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(550, 450), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Child_Should_Be_Measured_With_MaxAutoSizeHint_If_SizeToContent_Is_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1200, 1000));

                    var child = new ChildControl();
                    var target = new Window(windowImpl.Object)
                    {
                        Width = 100,
                        Height = 50,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    target.Show();

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(1200, 1000), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Should_Not_Have_Offset_On_Bounds_When_Content_Larger_Than_Max_Window_Size()
            {
                // Issue #3784.
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    var clientSize = new Size(200, 200);
                    var maxClientSize = new Size(480, 480);

                    windowImpl.Setup(x => x.Resize(It.IsAny<Size>(), It.IsAny<WindowResizeReason>()))
                        .Callback<Size, WindowResizeReason>((size, reason) =>
                    {
                        clientSize = size.Constrain(maxClientSize);
                        windowImpl.Object.Resized?.Invoke(clientSize, reason);
                    });

                    windowImpl.Setup(x => x.ClientSize).Returns(() => clientSize);

                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };
                    var target = new Window(windowImpl.Object)
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(new Size(400, 480), target.Bounds.Size);

                    // Issue #3784 causes this to be (0, 160) which makes no sense as Window has no
                    // parent control to be offset against.
                    Assert.Equal(new Point(0, 0), target.Bounds.Position);
                }
            }

            [Fact]
            public void Width_Height_Should_Not_Be_NaN_After_Show_With_SizeToContent_Manual()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.Manual,
                        Content = child
                    };

                    Show(target);

                    // Values come from MockWindowingPlatform defaults.
                    Assert.Equal(800, target.Width);
                    Assert.Equal(600, target.Height);
                }
            }

            [Fact]
            public void Width_Height_Should_Not_Be_NaN_After_Show_With_SizeToContent_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    target.GetObservable(Window.WidthProperty).Subscribe(x => { });

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);
                }
            }

            [Fact]
            public void MaxWidth_And_MaxHeight_Should_Be_Respected_With_SizeToContent_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new ChildControl();

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        MaxWidth = 300,
                        MaxHeight = 700,
                        Content = child,
                    };

                    Show(target);

                    Assert.Equal(new[] { new Size(300, 700) }, child.MeasureSizes);
                }
            }

            [Fact]
            public void SizeToContent_Should_Not_Be_Lost_On_Show()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }

            [Fact]
            public void SizeToContent_Should_Not_Be_Lost_On_Scaling_Change()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 209,
                        Height = 117,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    // Size before and after DPI change is a real-world example, with size after DPI
                    // change coming from Win32 WM_DPICHANGED.
                    target.PlatformImpl.ScalingChanged(1.5);
                    target.PlatformImpl.Resized(
                        new Size(210.66666666666666, 118.66666666666667),
                        WindowResizeReason.DpiChange);

                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }

            [Fact]
            public void Width_Height_Should_Be_Updated_When_SizeToContent_Is_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    child.Width = 410;
                    target.LayoutManager.ExecuteLayoutPass();

                    Assert.Equal(410, target.Width);
                    Assert.Equal(800, target.Height);
                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }

            [Fact]
            public void Setting_Width_Should_Resize_WindowImpl()
            {
                // Issue #3796
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window()
                    {
                        Width = 400,
                        Height = 800,
                    };

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    target.Width = 410;
                    target.LayoutManager.ExecuteLayoutPass();

                    var windowImpl = Mock.Get(target.PlatformImpl);
                    windowImpl.Verify(x => x.Resize(new Size(410, 800), WindowResizeReason.Application));
                    Assert.Equal(410, target.Width);
                }
            }


            [Fact]
            public void User_Resize_Of_Window_Width_Should_Reset_SizeToContent()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = new Canvas
                        {
                            Width = 400,
                            Height = 800,
                        },
                    };

                    Show(target);
                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    target.PlatformImpl.Resized(new Size(410, 800), WindowResizeReason.User);

                    Assert.Equal(410, target.Width);
                    Assert.Equal(800, target.Height);
                    Assert.Equal(SizeToContent.Height, target.SizeToContent);
                }
            }

            [Fact]
            public void User_Resize_Of_Window_Height_Should_Reset_SizeToContent()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = new Canvas
                        {
                            Width = 400,
                            Height = 800,
                        },
                    };

                    Show(target);
                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    target.PlatformImpl.Resized(new Size(400, 810), WindowResizeReason.User);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(810, target.Height);
                    Assert.Equal(SizeToContent.Width, target.SizeToContent);
                }
            }

            [Fact]
            public void Window_Resize_Should_Not_Reset_SizeToContent_If_CanResize_False()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        CanResize = false,
                        Content = new Canvas
                        {
                            Width = 400,
                            Height = 800,
                        },
                    };

                    Show(target);
                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    target.PlatformImpl.Resized(new Size(410, 810), WindowResizeReason.Unspecified);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);
                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }
            
            [Fact]
            public void IsVisible_Should_Open_Window()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window();
                    var raised = false;
                    
                    target.Opened += (s, e) => raised = true;
                    target.IsVisible = true;

                    Assert.True(raised);
                }
            }
            
            [Fact]
            public void Hiding_DialogWindow_Should_Complete_Task()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var parent = new Window();
                    parent.Show();
                    
                    var target = new Window();

                    var task = target.ShowDialog<bool>(parent);

                    target.IsVisible = false;

                    Assert.True(task.IsCompletedSuccessfully);
                }
            }

            [Fact]
            public void Show_Works_When_Min_Dimension_Greater_Than_Max()
            {
                using var app = UnitTestApplication.Start(TestServices.StyledWindow);

                var target = new Window
                {
                    MinWidth = 100,
                    MaxWidth = 80,
                    MinHeight = 200,
                    MaxHeight = 180
                };

                Show(target);

                Assert.Equal(100, target.Width);
                Assert.Equal(200, target.Height);
            }
            
            protected virtual void Show(Window window)
            {
                window.Show();
            }
        }

        public class DialogSizingTests : SizingTests
        {
            protected override void Show(Window window)
            {
                var owner = new Window();
                owner.Show();
                window.ShowDialog(owner);
            }
        }

        private static Mock<IWindowImpl> CreateImpl()
        {
            var screen1 = new Mock<Screen>(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns(screen1.Object);

            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

            return windowImpl;
        }

        private class ChildControl : Control
        {
            public List<Size> MeasureSizes { get; } = new List<Size>();

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureSizes.Add(availableSize);
                return base.MeasureOverride(availableSize);
            }
        }

        private class TopmostWindow : Window
        {
            static TopmostWindow()
            {
                TopmostProperty.OverrideDefaultValue<TopmostWindow>(true);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CustomControl
{
    /// <summary>
    ///内部元素可以拖拽在其中拖拽、旋转
    /// </summary>
    public class DragPanel : Canvas
    {
        private bool isMove = false;
        public event EventHandler<UIElement> DeletedChild;
        private Ellipse rotationEllipse = new Ellipse();
        public bool Locked { get; set; } //锁定以后，可以禁止所有内部元素的操作
        public DragPanel()
        {
            InitRotationArea();
            this.Background = Brushes.Transparent;
            this.MouseMove += Moving;
            this.MouseLeftButtonUp += StopMove;
            this.Loaded += (s, e) =>
            {
                this.Children.Insert(0, rotationEllipse);
            };
        }

        public static bool GetCanDrag(DependencyObject obj)
        {
            return (bool)obj.GetValue(CanDragProperty);
        }
        public static void SetCanDrag(DependencyObject obj, bool value)
        {
            obj.SetValue(CanDragProperty, value);
        }
        /// <summary>
        /// 元素是否可以拖拽，附加到子元素上
        /// </summary>
        public static readonly DependencyProperty CanDragProperty =
            DependencyProperty.RegisterAttached("CanDrag", typeof(bool), typeof(DragPanel), new PropertyMetadata(true));

        public static bool GetCanRotation(DependencyObject obj)
        {
            return (bool)obj.GetValue(CanRotationProperty);
        }
        public static void SetCanRotation(DependencyObject obj, bool value)
        {
            obj.SetValue(CanRotationProperty, value);
        }
        /// <summary>
        /// 元素是否可以旋转，附加到子元素上
        /// </summary>
        public static readonly DependencyProperty CanRotationProperty =
            DependencyProperty.RegisterAttached("CanRotation", typeof(bool), typeof(DragPanel), new PropertyMetadata(true));

        /// <summary>
        /// 元素是否可以被删除，附加到子元素上(若子元素需要自己处理删除事件，请将此值设置为false)
        /// </summary>
        public static bool GetCanDelete(DependencyObject obj)
        {
            return (bool)obj.GetValue(CanDeleteProperty);
        }
        public static void SetCanDelete(DependencyObject obj, bool value)
        {
            obj.SetValue(CanDeleteProperty, value);
        }
        public static readonly DependencyProperty CanDeleteProperty =
            DependencyProperty.RegisterAttached("CanDelete", typeof(bool), typeof(DragPanel), new PropertyMetadata(false));

        /// <summary>
        /// 拖拽或是旋转完成后触发
        /// </summary>
        public static readonly RoutedEvent TransformCompleteEvent = EventManager.RegisterRoutedEvent("TransformComplete", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DragPanel));
        public event RoutedEventHandler TransformComplete
        {
            add
            {
                AddHandler(TransformCompleteEvent, value);
            }
            remove
            {
                RemoveHandler(TransformCompleteEvent, value);
            }
        }

        private void AddDeleteContextMenu(FrameworkElement child)
        {
            if (child.ContextMenu != null)
            {
                foreach (MenuItem item in child.ContextMenu.Items)
                {
                    if (item.Header != null && item.Header.ToString() == "删除")
                        return;
                }
            }
            MenuItem menuitem = new MenuItem();
            menuitem.Header = "删除";
            menuitem.Click += (s, e) =>
            {
                UIElement deleteElement = ((ContextMenu)(((HeaderedItemsControl)(e.Source)).Parent)).PlacementTarget;
                if (deleteElement != null)
                {
                    if (this.Children.Contains(deleteElement))
                        this.Children.Remove(deleteElement);
                    if (DeletedChild != null)
                        DeletedChild(this, deleteElement);
                    rotationEllipse.Visibility = Visibility.Collapsed;
                    SelectElement = null;
                }
            };
            if (child.ContextMenu == null) child.ContextMenu = new ContextMenu();
            child.ContextMenu.Items.Add(menuitem);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            var elementAdd = visualAdded as FrameworkElement;
            if (elementAdd != null)
            {
                double x = Canvas.GetLeft(elementAdd);
                double y = Canvas.GetTop(elementAdd);
                //if (isMove)
                //    LimitElement(ref x, ref y, elementAdd);
                Canvas.SetLeft(elementAdd, x);
                Canvas.SetTop(elementAdd, y);
                elementAdd.MouseLeftButtonDown -= elementAdd_BeginMove;
                elementAdd.MouseLeftButtonDown += elementAdd_BeginMove;
                elementAdd.Loaded -= elementAdd_Loaded;
                elementAdd.Loaded += elementAdd_Loaded;
            }
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        void elementAdd_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                if (DragPanel.GetCanDelete((FrameworkElement)sender))
                    AddDeleteContextMenu((FrameworkElement)sender);
            }
        }

        private FrameworkElement _selectElement;
        public FrameworkElement SelectElement
        {
            get
            {
                return _selectElement;
            }
            set
            {
                _selectElement = value;
                if (SelectElement == null)
                {
                    rotationEllipse.Visibility = Visibility.Collapsed;
                    foreach (var c in this.Children)
                    {
                        var d = c as FrameworkElement;
                        if (d != null && d.ContextMenu != null)
                        {
                            d.ContextMenu.Visibility = Visibility.Collapsed;
                            Panel.SetZIndex(d, 0);
                        }
                    }
                    return;
                }
                if (DragPanel.GetCanRotation(SelectElement))
                {
                    FrameworkElement f = SelectElement as FrameworkElement;
                    rotationEllipse.Visibility = Visibility.Visible;
                    double d = rotationEllipse.Width;
                    if (!double.IsNaN(SelectElement.ActualWidth) && !double.IsNaN(SelectElement.ActualHeight))
                    {
                        d = Math.Sqrt(Math.Pow(SelectElement.ActualWidth, 2) + Math.Pow(SelectElement.ActualHeight, 2));
                        rotationEllipse.Width = d + 120;
                        rotationEllipse.Height = d + 120;
                    }
                    if (rotationEllipse.Width > Math.Min(this.ActualWidth, this.ActualHeight))
                    {
                        rotationEllipse.Width = Math.Min(this.ActualWidth, this.ActualHeight);
                        rotationEllipse.Height = Math.Min(this.ActualWidth, this.ActualHeight);
                    }
                    double left = Canvas.GetLeft(SelectElement), top = Canvas.GetTop(SelectElement);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    Canvas.SetLeft(rotationEllipse, SelectElement.ActualWidth / 2 - rotationEllipse.Width / 2 + left);
                    Canvas.SetTop(rotationEllipse, SelectElement.ActualHeight / 2 - rotationEllipse.Height / 2 + top);
                }
                foreach (var c in this.Children)
                {
                    var d = c as FrameworkElement;
                    if (d != null && d.ContextMenu != null)
                    {
                        if (d == SelectElement)
                        {
                            d.ContextMenu.IsOpen = false;
                            d.ContextMenu.Visibility = Visibility.Visible;
                            Panel.SetZIndex(d, 1); //显示在最上面
                        }
                        else
                        {
                            d.ContextMenu.Visibility = Visibility.Collapsed;
                            Panel.SetZIndex(d, 0);
                        }
                    }
                }
            }
        }

        private double offsetX, offsetY;
        private Point? transformMouseStartPosition = null;//移动、旋转时鼠标的位置
        private Point? moveStartPosition = null;//开始移动时元素左上角的位置
        void elementAdd_BeginMove(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Locked) return;
            SelectElement = sender as FrameworkElement;
            if (SelectElement == null) return;
            if (e.ClickCount == 1)
                isMove = true;
            isRotation = false;
            transformMouseStartPosition = e.GetPosition(this);
            offsetX = e.GetPosition(SelectElement).X;
            offsetY = e.GetPosition(SelectElement).Y;
            moveStartPosition = new Point(Canvas.GetLeft(SelectElement), Canvas.GetTop(SelectElement));
        }

        void Moving(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isMove && Mouse.LeftButton == MouseButtonState.Released)//鼠标弹起
            {
                StopMove(sender, e);
                return;
            }
            if (SelectElement == null) return;
            if (isMove && SelectElement != null)
            {
                if (!DragPanel.GetCanDrag(SelectElement)) return;
                double oldLeft = Canvas.GetLeft(SelectElement);
                double oldTop = Canvas.GetTop(SelectElement);
                double elementX = e.GetPosition(this).X - offsetX;
                double elementY = e.GetPosition(this).Y - offsetY;
                //LimitElement(ref elementX, ref elementY, SelectElement);
                Canvas.SetLeft(SelectElement, elementX);
                Canvas.SetTop(SelectElement, elementY);
                Canvas.SetLeft(rotationEllipse, SelectElement.ActualWidth / 2 - rotationEllipse.Width / 2 + elementX);
                Canvas.SetTop(rotationEllipse, SelectElement.ActualHeight / 2 - rotationEllipse.Height / 2 + elementY);
            }
        }

        void StopMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isMove || isRotation)
            {
                Point p = e.GetPosition(this);
                if (transformMouseStartPosition != null && (Math.Abs(transformMouseStartPosition.Value.X - p.X) > 0 || Math.Abs(transformMouseStartPosition.Value.Y - p.Y) > 0))
                {
                    SelectElement.RaiseEvent(new RoutedEventArgs(TransformCompleteEvent));
                    transformMouseStartPosition = null;
                }
            }
            isMove = false;
            isRotation = false;
        }

        private void LimitElement(ref double pointX, ref double pointY, FrameworkElement element)
        {
            if (double.IsNaN(pointX) || double.IsNaN(pointY))
            {
                pointX = 0;
                pointY = 0;
            }
            if (pointX < 0)
                pointX = 0;
            if (pointX >= this.ActualWidth - element.ActualWidth)
                pointX = this.ActualWidth - element.ActualWidth;
            if (pointY < 0)
                pointY = 0;
            if (pointY >= this.ActualHeight - element.ActualHeight)
                pointY = this.ActualHeight - element.ActualHeight;
        }

        private bool isRotation = false;//旋转
        private Point oldpoint;
        private Point centerpoint;
        private double rotationangle = 0;
        private void InitRotationArea()
        {
            rotationEllipse.Width = 300;
            rotationEllipse.Height = 300;
            //rotationEllipse.Fill = Brushes.Red;
            rotationEllipse.Stroke = Brushes.Red;//用圆环替代圆，防止误操作
            rotationEllipse.StrokeThickness = 50;
            rotationEllipse.Opacity = 0.3;
            rotationEllipse.Tag = "rotationEllipse";
            rotationEllipse.Visibility = Visibility.Collapsed;
            rotationEllipse.MouseLeftButtonDown += rotationEllipse_MouseLeftButtonDown;
            rotationEllipse.MouseMove += rotationEllipse_MouseMove;
        }
        void rotationEllipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isRotation || SelectElement == null || !DragPanel.GetCanRotation(SelectElement)) return;
            Point newpoint = e.GetPosition(rotationEllipse);
            Point rotationEllipseCenter = new Point(rotationEllipse.Width / 2, rotationEllipse.Height / 2);
            double angle = Vector.AngleBetween(newpoint - rotationEllipseCenter, oldpoint - rotationEllipseCenter);//返回角度单位为度，逆时针方向为正（按标准数学定义）
            rotationangle += angle;
            SelectElement.RenderTransform = new RotateTransform(-rotationangle, SelectElement.ActualWidth / 2, SelectElement.ActualHeight / 2);//旋转变换中顺时针为正
            oldpoint = newpoint;
        }
        void rotationEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRotation = true;
            isMove = false;
            rotationangle = 0;
            centerpoint = new Point(Canvas.GetLeft(rotationEllipse) + rotationEllipse.Width / 2, Canvas.GetTop(rotationEllipse) + rotationEllipse.Height / 2);
            oldpoint = e.GetPosition(rotationEllipse);
            transformMouseStartPosition = e.GetPosition(this);
            e.Handled = true;
        }

        public void ResetState()
        {
            SelectElement = null;
            isRotation = false;
            isMove = false;
            rotationEllipse.Visibility = Visibility.Collapsed;
        }

    }
}

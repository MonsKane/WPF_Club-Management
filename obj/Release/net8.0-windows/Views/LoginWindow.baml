   M S B A M L   `   `   `  ���� V  RClub Management Application, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullK*clr-namespace:ClubManagementApp.ConvertersClubManagementApp.Converters  )    #ClubManagementApp.Views.LoginWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     A
converters*clr-namespace:ClubManagementApp.Converters       � Title$=  7Hệ Thống Quản Lý Câu Lạc Bộ - Đăng Nhập��$	��850��$
��1000�� � WindowStartupLocation$ CenterScreen=� � WindowStyle$
 None=� � AllowsTransparency$
 True�� � 
ResizeMode$ 	CanResize=�$	��800��$	��700������  �� 
StartPoint$	 0,0&� �� EndPoint$	 1,1&�.���  � Color$ #667eea�� � Offset$ 0Q�� $ #764ba2��$ 1Q�	 � 	Resources	     BooleanToVisibilityConverterF   @ClubManagementApp.Converters.InverseBooleanToVisibilityConverter ' #InverseBooleanToVisibilityConverter  ModernTextBox
 �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius ��Name �� Triggers P� Property P� Value  ModernPasswordBox  ModernButton ��Name J� RenderTransformOrigin �� ScaleX �� ScaleY J� Effect3  -System.Windows.Media.Effects.DropShadowEffect   Color   
BlurRadius   ShadowDepth   Opacity N� EnterActions �� 
TargetName �� TargetProperty	 W� To  g� Duration! N� ExitActions��%7	  &	        &	      &	 
     &	 �    &	 (    ��   �� "
 ��.���  � ��
 �������  � �� �#e0e6ed��  � ��$ 2q���  � ��$ 15,12q���  � ��$ 14(���  � ��$ Segoe UI)���  � �� �� " ��.R��� #��z��#��z��#��z��$ 8��._��� $ PART_ContentHost��#��z�� P�  �@J�	IsFocused$
 True��. ���  � �� �#667eeaP�  �@J�IsMouseOver$
 True��. ���  � �� �#a0a8b8�� "
 G�.���  � ��
 �������  � �� �#e0e6ed��  � ��$ 2q���  � ��$ 15,12q���  � ��$ 14(���  � ��$ Segoe UI)���  � �� �� " G�.R��� #��z��#��z��#��z��$ 8��._��� $ PART_ContentHost��#��z�� P�  �@J�	IsFocused$
 True��. ���  � �� �#667eeaP�  �@J�IsMouseOver$
 True��. ���  � �� �#a0a8b8�� "
 ��.���  � �� �� $	 0,0&�$	 1,1&�.��� $ #4facfe��$ 0Q�� $ #00f2fe��$ 1Q���  � ��
 �������  � ��$ 16(���  � ��$
 Bold"���  � ��$ Segoe UI)���  � ��$
 0,25q���  � ��$ 50����  � ��$ 0q���  �@�Cursor$
 Hand����  � �� �� " ��.R��� $ border��#��z��$ 12��#��z��#��z��$ 0.5,0.5&�z��� scaleTransform��$ 1Q�$ 1Q�   shadowEffect��$ #4facfe��$ 15Q�$ 4Q�$	 0.4Q�._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True�� �� .���� .��W� $ scaleTransform��$ ScaleX� 1.05$  0:0:0.2D�W� $ scaleTransform��$ ScaleY� 1.05$  0:0:0.2D�W� $ shadowEffect��$ 
BlurRadius� 20$  0:0:0.2D�! �� .���� .��W� $ scaleTransform��$ ScaleX� 1$  0:0:0.2D�W� $ scaleTransform��$ ScaleY� 1$  0:0:0.2D�W� $ shadowEffect��$ 
BlurRadius� 15$  0:0:0.2D�. ���  � �� �� $	 0,0&�$	 1,1&�.��� $ #43a3f5��$ 0Q�� $ #00d4ff��$ 1Q�P�  � ��$
 True�� �� .���� .��W� $ scaleTransform��$ ScaleX� 0.98$  0:0:0.1D�W� $ scaleTransform��$ ScaleY� 0.98$  0:0:0.1D�! �� .���� .��W� $ scaleTransform��$ ScaleX� 1$  0:0:0.1D�W� $ scaleTransform��$ ScaleY� 1$  0:0:0.1D�. ���  � �� �� $	 0,0&�$	 1,1&�.��� $ #3a9ae3��$ 0Q�� $ #00bfed��$ 1Q�.���� $ 15��$��20q�   $ Black��$ 20Q�$ 5Q�$	 0.3Q�._�� " � RowDefinitions" �� $
��Auto ��� $��* �.+��� -   $��0��
������ $��40��$	��Top=�._�� # � ColumnDefinitions# �� $��* ��� $
��Auto �.+��� $��0��$��Club Management System��$��14(�$��Medium"�$��Segoe UI)�
���   �$��Center=�$��20,0,0,0q��� $��1��$ �� Orientation$$ 
Horizontal=�$��0,5,15,0q�.+��� -   $��30��$��30��$	��2,0q�
������ $��0q�
�������$��14(�$��Segoe UI Symbol)�$
��🗕��% � Cursor$
% Hand������ "
 ��.���  � �� �� " ��.R��� #��z��$ 4��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � �� �	#40FFFFFF�� -   & ��Name$& MaximizeRestoreButton��$��30��$��30��$	��2,0q�
������ $��0q�
�������$��14(�$��Segoe UI Symbol)�$
��🗖��$
% Hand������ "
 ��.���  � �� �� " ��.R��� #��z��$ 4��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � �� �	#40FFFFFF�� -   $��30��$��30��$	��2,0q�
������ $��0q�
�������$��14(�$��Segoe UI Symbol)�$
��🗙��$
% Hand������ "
 ��.���  � �� �� " ��.R��� #��z��$ 4��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � �� �#ff4757�� $��0��$��	0,60,0,40q�$��Center=�.+��� $	��100��$	��100��$��0,0,0,25q�$ 20��
�������' �� Padding$' 8q�   $ Black��(   	Direction$	( 270Q�$ 4Q�$	 0.3Q�$ 15Q�._�(� .���� $	��100��$	��100��.+�A� ) �� Left$) 5��
* �� Top$* 5��$��90��$��90�����#3B82F6���#1E40AF$��2���� $) 50��$* 50��.+��� $	) -18��$* -5��$��36��$��25�����#FFFFFF+ �� RadiusX$+ 2��, �� RadiusY$, 2��� - � Points#- �   �N������ �����O�������#EF4444���#DC2626$��1���� $) -4��$* 8��$��8��$��12�����#8B5CF6$+ 1��$, 1��A� $	) 1.2��$
* 13.2��$	��1.6��$	��1.6�����#FFFFFF�� $	) -14��$* -1��$��6��$��6�����#60A5FA$+ 1��$, 1���� $) 8��$* -1��$��6��$��6�����#60A5FA$+ 1��$, 1���� 	. �� X1$	. -11��	/ �� Y1$/ -1��	0 �� X2$	0 -11��	1 �� Y2$1 5�����#FFFFFF$	��0.5���� $	. -14��$/ 2��$0 -8��$1 2�����#FFFFFF$	��0.5���� $. 11��$/ -1��$0 11��$1 5�����#FFFFFF$	��0.5���� $. 8��$/ 2��$0 14��$1 2�����#FFFFFF$	��0.5���� $) -8��$	* -25��.+�A� $
) -2.5��$
* -2.5��$��5��$��5�����#FCD34DA� $) -3��$* 2��$��6��$��8�����#10B981�� $) 0��$	* -25��.+�A� $
) -2.5��$
* -2.5��$��5��$��5�����#FCD34DA� $) -3��$* 2��$��6��$��8�����#F59E0B�� $) 8��$	* -25��.+�A� $
) -2.5��$
* -2.5��$��5��$��5�����#FCD34DA� $) -3��$* 2��$��6��$��8�����#EF4444A� $) 18��$* 23��$��4��$��4�����#FBBF242 J� Opacity$	2 0.7Q�A� $) 77��$* 28��$��3��$��3�����#34D399$	2 0.7Q�A� $
) 21.4��$
* 71.4��$	��3.6��$	��3.6�����#F472B6$	2 0.7Q�A� $
) 70.6��$
* 65.6��$	��4.4��$	��4.4�����#A78BFA$	2 0.7Q��� $-��'Hệ Thống Quản Lý Câu Lạc Bộ��$��32(�$
��Bold"�$��Segoe UI)�
�������$��Center=�$��0,0,0,8q��� $[��UChào mừng bạn đến với hệ thống quản lý câu lạc bộ hiện đại��$��16(�$��Segoe UI)�
�������$��Center=�$	2 0.9Q�$��0,0,0,5q��� $T��NĐăng nhập để tiếp tục và khám phá các tính năng tuyệt vời��$��14(�$��Segoe UI)�
�������$��Center=�$	2 0.8Q��� $��1��$��
80,0,80,80q�
�������$ 15�����#e0e6ed$��1q�   $ Black��$ 15Q�$ 3Q�$	 0.1Q�._��� $��50,40q�$��Center=�.+��� $��Email��$��Medium"�$��Segoe UI)�$��0,0,0,8q�
���   ��� -   3 ��Name$3 EmailTextBox��#��[ $��0,0,0,25q����� *Email+4 �� UpdateSourceTrigger$4 PropertyChanged=��� $��Mật Khẩu��$��Medium"�$��Segoe UI)�$��0,0,0,8q�
���   �G� -   5 G�Name$5 PasswordBox��#��[ $��0,0,0,25q��� ���#fee���#f56565$��1q�$ 6��$
' 12,8q�$��0,0,0,15q�y��� *
HasError+6 �� 	Converter#6 [  ._��� 
���   �$��13(�$��Segoe UI)����� *ErrorMessage+�� #��[ $��0,15,0,0q����� *LoginCommand+���� $$ 
Horizontal=�$��Center=�.+��� $��Đăng Nhập��y��� *	IsLoading+#6 [ �� $$ 
Horizontal=�y��� *	IsLoading+#6 [  .+��� $��Đang đăng nhập...��$��0,0,10,0q�A� $��16��$��16��
�������$��2��7 �� StrokeDashArray$	7 3,1R�$ 0.5,0.5&�z��� 8 � Triggers8 :� 9 :� RoutedEvent$9 Loaded��.[��� .���� : g� RepeatBehavior$: Forever��.��W� $ RenderTransform.Angle�; W� From; 0 360$  0:0:1D�
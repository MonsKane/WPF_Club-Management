   M S B A M L   `   `   `  ����  Club Management Application)    #ClubManagementApp.Views.LoginWindow   5   	   W SPresentationCore, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$$  Club Management System - Login��5   	   $	��400��5   	   $	��500��6    � WindowStartupLocation$ CenterScreen=�5   	    � 
ResizeMode$ NoResize=�5   	   ���#f0f0f0.��5
      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5   
   �� $��0��6   ���#2c3e50 �� Padding$ 20q�63   ._�5      �� $��Club Management System��6   $��24(�5      $
��Bold"�5      
�������$��Center=�5      5      �� 5   
   $��1��6   $��50q�6   
������� �� CornerRadius$ 10��5      ���#bdc3c7$��1q�5      ._�5      �� $��40q�6   $��Center=�6%   .+�5!      �� $��Login��6   $��20(�5"      $
��Bold"�5#      $��Center=�5$      $��0,0,0,30q�5%      ���#2c3e505&      �� 5)      $��Email:��6   $��SemiBold"�5*      $��0,0,0,5q�5+      ���#34495e5,      �� 5-      -   5         ��Name$ EmailTextBox��5-      $��10q�5/      $��14(�50      $��0,0,0,20q�51      ���#bdc3c7$��1q�53      ��5.      �� *Email+ �� UpdateSourceTrigger$ PropertyChanged=�53      �� 56      $��	Password:��6   $��SemiBold"�57      $��0,0,0,5q�58      ���#34495e59      G� 5:      -   5         G�Name$ PasswordBox��5:      $��10q�5;      $��14(�5<      $��0,0,0,20q�5=      ���#bdc3c7$��1q�5?      �� 5B      
���  ��$��12(�5D      $��0,0,0,10q�5E      ��5B      �� *ErrorMessage+y�5F      �� *
HasError+    BooleanToVisibilityConverter	 �� 	Converter#	 [  �� 5I      $��Login��6   ���#3498db
�������$��16(�5N      $
��Bold"�5O      $
��0,12q�5P      $��0,10,0,0q�5Q      $��0q�5R      
 � Cursor$

 Hand��5S      ��5J      �� *LoginCommand+��5K      ��  �� ElementName$ PasswordBox����5T      �� 5U       �� 
TargetType" ��6    .�5V      ��  �� Property � �� �� Value 5W   "   �� 5X   &    �� 
TargetType" ��66   .R�5Y   *   �� #��z��61   $ 5��5Z   1   #��z��5[   1   #��z��5\   1   ._�5]   .   �� $��Center=�6?   $��Center=�5^   ?   5_   +    �� Triggers 5`   *   P� 5a   .    P� Property �@J�IsMouseOver P� Value$
 True��6M   . �5b   2   ��  � �� �#2980b96O   5c   /   5e   '   5g      5h      5j      5l      5m      5n      5o      
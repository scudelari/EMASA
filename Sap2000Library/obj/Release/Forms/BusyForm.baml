   M S B A M L   `   `   `  ���� D  @S2KHelper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null1clr-namespace:S2KHelper.FormsS2KHelper.Forms      S2KHelper.Forms.BusyForm   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     /localclr-namespace:S2KHelper.Forms     -     � 
ResizeMode$  NoResize=� � ShowInTaskbar .   � WindowStartupLocation$ CenterOwner=� � SizeToContent$ Height=�$	��600�� � Icon$8 2pack://siteoforigin:,,,/Resources/EMS_App_Icon.ico�� � Title �� *	TitleText+.���  � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto �.+��� $��	10 5 10 5q�
���OO/�$��1q� �� CornerRadius$ 3��$��0�� �� Padding$ 5 5 5 5q�._��� 	 �� Orientation$	 Vertical=�.+��� 
 �� TextAlignment$

 Left=�$	��Top=��� ��� *MessageText+�� 
����=H�$��0 5 0 0q�$

 Left=�$	��Top=�y��� *CurrentElementVisibility+�� .��`�  �� StringFormat$ {0}: {1}��.1���  �� Path$ ElementType��� $ CurrentElementName�.�� $��1��$��10q�.+�� $��30�� � Minimum$ 0Q� � Maximum$	 100Q�$��Stretch=� � Value �� *ProgressCurrentProgress+ � IsIndeterminate �� *ProgressIsIntermediate+�� $��16(�$��Center=�$��Center=�
���OO/����� *ProgressCurrentProgress+$ {0:0}%���� -    ��Name$ CancelButton��$	��150��$��30��$��Center=�$��2��$��5q����� *ButtonCaption+y��� *ButtonVisibility+
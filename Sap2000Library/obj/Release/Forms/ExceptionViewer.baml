   M S B A M L   `   `   `  ����   	S2KHelper    S2KHelper.ExceptionViewer   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -   $��ExpressionViewerWindow��$	��800��$	��600��$	��350��$	��200��  � WindowStartupLocation$  CenterOwner=�.��� -   $��mainGrid�� � RowDefinitions �� $��* ��� $
��auto � � ColumnDefinitions �� -    �Name$ treeCol��$
��Auto �$	��250��$��90���� -   $ textCol��$��* �$��90��.+�R� -   $��	treeView1��$��5q��� $��5��$��0,5,0,5q�$��1�� �� ResizeBehavior$ CurrentAndNext=��� $��0,5,5,5q�$��1������  �� ElementName$ 	treeView1�� �� Path$ BorderThickness����� $ 	treeView1��$ BorderBrush�._�,� -   $��	docViewer��$ ,� HorizontalScrollBarVisibility$
 Auto=�" ,� VerticalScrollBarVisibility$
 Auto=�� $��1��$��2��$��Right=� �� $
��auto ��� $
��auto ��� $
��auto �.+��� -   $��chkWrap��$��
_Wrap text��$��16��$��Right=�$��0,0,23,0q�$��center=�	 d� 	IsChecked$
	 TrueY�$y�Hidden=��� -   $��btnCopy��$��_Copy All To Clipboard��$
��15,0q�$��1,3,4,6q�$��1��$��Center=�$��23���� -	   $��btnClose��$��Close��$��2��$
��15,0q�$��1,3,4,6q�$��Center=�$��23��
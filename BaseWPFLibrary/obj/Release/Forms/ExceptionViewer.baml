   M S B A M L   `   `   `  ����   BaseWPFLibrary*    $BaseWPFLibrary.Forms.ExceptionViewer   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -   $��ExpressionViewerWindow��$	��800��$	��600��$	��350��$	��200��  � WindowStartupLocation$  CenterOwner=�.��� -   $��mainGrid�� � RowDefinitions �� $��* ��� $
��auto � � ColumnDefinitions �� -    �Name$ treeCol��$
��Auto �$	��100��$	��300���� -   $ textCol��$��* �$��90��.+�R� -   $��	treeView1��$��5q��� $��1��$��5��$��0,5,0,5q� �� ResizeBehavior$ CurrentAndNext=��� $��1��$��0,5,5,5q�����  �� ElementName$ 	treeView1�� �� Path$ BorderBrush����� $ 	treeView1��$ BorderThickness�._�,� -   $��	docViewer��$ ,� HorizontalScrollBarVisibility$
 Auto=�" ,� VerticalScrollBarVisibility$
 Auto=�� $��1��$��2��$��Right=� �� $
��auto ��� $
��auto ��� $
��auto �.+��� -   $��chkWrap��$��16��$��0,0,23,0q�$��Right=�$��center=�$��
_Wrap text��	 d� 	IsChecked$
	 TrueY�$y�Hidden=��� -   $��btnCopy��$��1��$��23��$��1,3,4,6q�$
��15,0q�$��Center=�$��_Copy All To Clipboard���� -	   $��btnClose��$��2��$��23��$��1,3,4,6q�$
��15,0q�$��Center=�$��Close��
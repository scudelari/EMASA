   M S B A M L   `   `   `  ����  BaseWPFLibrary*    $BaseWPFLibrary.Forms.ExceptionViewer   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      -   5        $��ExpressionViewerWindow��5      $	��800��5      $	��600��5      $	��350��5      $	��200��5	        � WindowStartupLocation$  CenterOwner=�5      .��5      � -   5        $��mainGrid��5       � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��auto �6    � ColumnDefinitions 5   
   �� 5      -   5         �Name$ treeCol��5      $
��Auto �5      $	��100��5      $	��300��5      �� 5      -   5        $ textCol��5      $��* �5      $��90��5      .+�5   
   R� -   5        $��	treeView1��5      $��5q�5      5       �� 5!   
   $��1��5"      $��5��5#      $��0,5,0,5q�5$       �� ResizeBehavior$ CurrentAndNext=�5%      �� 5&   
   $��1��5'      $��0,5,5,5q�5(      ��5)      ��  �� ElementName$ 	treeView1�� �� Path$ BorderBrush���5*      �� $ 	treeView1��$ BorderThickness�._�5+      ,� -   5        $��	docViewer��5,      $ ,� HorizontalScrollBarVisibility$
 Auto=�5-      " ,� VerticalScrollBarVisibility$
 Auto=�5.      5/      � 50   
   $��1��51      $��2��52      $��Right=�53       54      �� 55      $
��auto �6#   �� 56      $
��auto �6#   �� 57      $
��auto �6#   .+�59      �� -   5        $��chkWrap��5:      $��16��5;      $��0,0,23,0q�5<      $��Right=�5=      $��center=�5>      $��
_Wrap text��5@      	 d� 	IsChecked$
	 TrueY�5A      $y�Hidden=�5C      �� 5D      -   5        $��btnCopy��5E      $��1��5F      $��23��5G      $��1,3,4,6q�5H      $
��15,0q�5I      $��Center=�5J      $��_Copy All To Clipboard��5L      �� 5M      -	   5        $��btnClose��5N      $��2��5O      $��23��5P      $��1,3,4,6q�5Q      $
��15,0q�5R      $��Center=�5S      $��Close��5U      5V      5W      5X      
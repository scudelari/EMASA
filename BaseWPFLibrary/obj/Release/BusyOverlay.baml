   M S B A M L   `   `   `  ���� I  EBaseWPFLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullU/clr-namespace:BaseWPFLibrary.AttachedProperties!BaseWPFLibrary.AttachedProperties  A%clr-namespace:BaseWPFLibrary.BindingsBaseWPFLibrary.Bindings  /clr-namespace:BaseWPFLibraryBaseWPFLibrary       BaseWPFLibrary.BusyOverlay   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     NattachedProperties/clr-namespace:BaseWPFLibrary.AttachedProperties     :bindings%clr-namespace:BaseWPFLibrary.Bindings     @d2http://schemas.microsoft.com/expression/blend/2008     .localclr-namespace:BaseWPFLibrary     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     $��Stretch=�$��Stretch=�.��� -     �Name$  BusyOverlay_ContentGrid��.+�� $��Stretch=�$��Stretch=�$y�Visible=����� *OverlayBackground+ J� Opacity �� *OverlayBackgroundOpacity+� $��Stretch=�$��Stretch=����� *ContentWindowMargin+.+�]� -    ]�Name$ BusyOverlay_InnerPanel��
�������1   +BaseWPFLibrary.Bindings.BusyOverlayBindings   I � DataContext# Z $ 1Q�.+��� $	��Top=� �� Orientation$ Vertical=�.+���  �� Padding$ 5q�
���  ��
�������$Y��S* This function uses MS Windows Interface Automation - Avoid meddling with the PC *�� �� TextAlignment$ Center=�y��� *AutomationWarning_Visibility+�� $ 5q����	#FF414163
�������$ Center=����� *Title+�� $��Bottom=�$ Vertical=�.+�� $��10q�.+�� $��30��$��Stretch=� � Maximum$	 100Q�	 � Minimum$	 0Q�
 � IsIndeterminate
 �� *ProgressIsIndeterminate+ � Value �� *ProgressCurrentProgress+�� $��Center=�$��Center=�$��16(�
���OO/����� *ProgressCurrentProgress+ �� StringFormat$ {0:0}%��y��� *ProgressTextVisibility+�� -    ��Name$ CancelButton��$	��150��$��30��$��5q�$��Center=����� *ButtonCaption+{��� *ButtonIsEnabled+y��� *ButtonVisibility+�� $ 5q�
���  ��
�������$Y��S* This function uses MS Windows Interface Automation - Avoid meddling with the PC *��$ Center=�y��� *AutomationWarning_Visibility+�� -    ��Name$ MessageBorder��$��1��$��	10,5,10,5q� �� Padding$ 5,5,5,5q�
���OO/�$��1q� �� CornerRadius$ 3��._�]� .+��� $	��Top=�$ Vertical=�.+��� $	��Top=�$
 Left=�$
��Wrap=��� $��Courier New)���� *MessageText+�� $��0,5,0,0q�$	��Top=�
���iii�$
 Left=�y��� *CurrentElementVisibility+�� $��Courier New)�.��`� $ {0}: {1}��.1���  �� Path$ ElementType��� $ CurrentElementName�.��� -   $ LongMessageBorder��$��0,5,0,0q�$��Stretch=����	#FF414163$��0,1,0,0q�y��� *LongReport_Visibility+._�� .+��� $��5q�$��Stretch=�:   4BaseWPFLibrary.AttachedProperties.AutoScrollBehavior   
AutoScroll . $
��Auto=�$��Visible=�.���� $��Stretch=�$��0q�$��Courier New)�
���iii� �� 
IsReadOnly .  �� TextAlignment$
 Left=� �� TextWrapping$ NoWrap=����� *LongTextHeight+���� *LongReport_Text+
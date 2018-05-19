# OBSOLETE - How to enable LayoutView mode in the GridControl in List Views


<p><strong>=============</strong><strong>=============</strong></p><p><strong>This e</strong><strong>xample is now obsolete. Re</strong><strong>fer to the </strong><strong>new </strong><a href="http://community.devexpress.com/blogs/eaf/archive/2012/12/18/xvideorental-real-world-application-rwa-the-overview.aspx"><strong><u>XVideoRental demo</u></strong></a><strong> </strong><strong>th</strong><strong>at </strong><strong>contains a working implementation for AdvBandedGridView and LayoutView. </strong><strong><br />
</strong><strong>The files can be found in Common.Win project.<br />
</strong><strong>==========================</strong></p><p><strong><br />
</strong><strong>IMPORTANT NOTES</strong></p><p><strong>1.</strong> The LayoutViewListEditor class implemented in this example is not a complete solution, but rather a starting point for creating a custom List Editor based on the XtraGrid's LayoutView and providing similar features as a standard GridListEditor. Since this custom List Editor may have issues, you will use it at your own risk. <br />
Refer to the sources of a standard GridListEditor class ("%ProgramFiles\DevExpress 201X.X\eXpressApp Framework\Sources\DevExpress.ExpressApp\DevExpress.ExpressApp.Win\Editors\GridListEditor.cs") and the product documentation for more information about implementing custom List Editors.<br />
You can also track <a href="https://www.devexpress.com/Support/Center/p/S19992">S19992</a> to be automatically notified when the complete solution is available.<br />
<strong>2.</strong> To see LayoutViewListEditor in action, <a href="http://community.devexpress.com/blogs/eaf/LayoutViewListEditor.zip"><u>download this video</u></a>.</p>


<h3>Description</h3>

<p>In this update I  have slightly refactored the code, added support for storing layout settings in the application model, supported automatic cards scrolling in lookup ListViews and also started adding support (see LayoutViewColumnChooserController) for a standard XAF customization form that allows to add new columns via the LayoutView designer. I hope that other XAF developers will find this example as a good starting point to their own List Editor&#39;s implementations.</p><p>Note that due to the complexity of the implemented List Editor and from the maintainability POV, I have not provided a VB.NET version of this example.</p>

<br/>



﻿Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Printing

Public Class AFPDFLibUtil

  'This uses an XPDF wrapper written by Jose Antonio Sandoval Soria of Guadalajara, México
  'The source is available at http://www.codeproject.com/KB/files/xpdf_csharp.aspx
  '
  'I have ported over to VB.NET select functionality from the C# PDF viewer in the above project

  Const RENDER_DPI As Integer = 150

  Public Shared Function GetPageFromPDF(ByVal filename As String, ByVal destPath As String, ByRef PageNumber As Integer, Optional ByVal DPI As Integer = RENDER_DPI, Optional ByVal Password As String = "", Optional ByVal searchText As String = "", Optional ByVal searchDir As SearchDirection = 0) As String
    GetPageFromPDF = ""
    Dim pdfDoc As New PDFLibNet.PDFWrapper
    pdfDoc.LoadPDF(filename)
    If Not Nothing Is pdfDoc Then
      pdfDoc.CurrentPage = PageNumber
      Dim outGuid As Guid = Guid.NewGuid()
      Dim output As String = destPath & "\" & outGuid.ToString & ".jpg"
      If searchText <> "" Then
        Dim lFound As Integer = 0
        If searchDir = SearchDirection.FromBeginning Then
          lFound = pdfDoc.FindFirst(searchText, PDFLibNet.PDFSearchOrder.PDFSearchFromdBegin, False, False)
        ElseIf searchDir = SearchDirection.Forwards Then
          lFound = pdfDoc.FindFirst(searchText, PDFLibNet.PDFSearchOrder.PDFSearchFromCurrent, False, False)
        ElseIf searchDir = SearchDirection.Backwards Then
          lFound = pdfDoc.FindFirst(searchText, PDFLibNet.PDFSearchOrder.PDFSearchFromCurrent, True, False)
        End If
        If lFound > 0 Then
          If searchDir = SearchDirection.FromBeginning Then
            PageNumber = pdfDoc.SearchResults(0).Page
          ElseIf searchDir = SearchDirection.Forwards Then
            If pdfDoc.SearchResults(0).Page > PageNumber Then
              PageNumber = pdfDoc.SearchResults(0).Page
            Else
              PageNumber = SearchForNextText(pdfDoc, searchText, PageNumber, searchDir)
            End If
          ElseIf searchDir = SearchDirection.Backwards Then
            If pdfDoc.SearchResults(0).Page < PageNumber Then
              PageNumber = pdfDoc.SearchResults(0).Page
            Else
              PageNumber = SearchForNextText(pdfDoc, searchText, PageNumber, searchDir)
            End If
          End If
        End If
      End If
      pdfDoc.ExportJpg(output, PageNumber, PageNumber, DPI, 90)
      While (pdfDoc.IsJpgBusy)
        Threading.Thread.Sleep(50)
      End While
      pdfDoc.Dispose()
      GetPageFromPDF = output
    End If
  End Function

  Public Shared Function SearchForNextText(ByRef pdfDoc As PDFLibNet.PDFWrapper, ByVal searchText As String, ByVal currentPage As Integer, ByVal searchDir As SearchDirection) As Integer
    If Not Nothing Is pdfDoc Then
SearchPDF:
      Dim lFound As Integer = 0
      If searchDir = SearchDirection.Forwards Then
        lFound = pdfDoc.FindNext(searchText)
      ElseIf searchDir = SearchDirection.Backwards Then
        lFound = pdfDoc.FindPrevious(searchText)
      End If
      If lFound > 0 Then
        If (pdfDoc.SearchResults(0).Page > currentPage And searchDir = SearchDirection.Forwards) _
        Or (pdfDoc.SearchResults(0).Page < currentPage And searchDir = SearchDirection.Backwards) Then
          Return pdfDoc.SearchResults(0).Page
        Else
          GoTo SearchPDF
        End If
      Else
        Return currentPage
      End If
    End If
  End Function


  Public Shared Function GetOptimalDPI(ByRef pdfDoc As PDFLibNet.PDFWrapper, ByRef oSize As Drawing.Size) As Integer
    GetOptimalDPI = 0
    If pdfDoc IsNot Nothing Then
      If pdfDoc.PageWidth > 0 And pdfDoc.PageHeight > 0 Then
        Dim DPIScalePercent As Single = 72 / pdfDoc.RenderDPI
        Dim picHeight As Integer = oSize.Height
        Dim picWidth As Integer = oSize.Width
        Dim docHeight As Integer = pdfDoc.PageHeight
        Dim docWidth As Integer = pdfDoc.PageWidth
        Dim dummyPicBox As New PictureBox
        dummyPicBox.Size = oSize
        If (picWidth > picHeight And docWidth < docHeight) Or (picWidth < picHeight And docWidth > docHeight) Then
          dummyPicBox.Width = picHeight
          dummyPicBox.Height = picWidth
        End If
        Dim HScale As Single = dummyPicBox.Width / (pdfDoc.PageWidth * DPIScalePercent)
        Dim VScale As Single = dummyPicBox.Height / (pdfDoc.PageHeight * DPIScalePercent)
        dummyPicBox.Dispose()
        If VScale > HScale Then
          GetOptimalDPI = Math.Floor(72 * HScale)
        Else
          GetOptimalDPI = Math.Floor(72 * VScale)
        End If
      End If
    End If
  End Function

  Public Shared Function GetImageFromPDF(ByRef pdfDoc As PDFLibNet.PDFWrapper, ByVal PageNumber As Integer, Optional ByVal DPI As Integer = RENDER_DPI) As System.Drawing.Image
    GetImageFromPDF = Nothing
    Try
      If pdfDoc IsNot Nothing Then
        pdfDoc.CurrentPage = PageNumber
        pdfDoc.CurrentX = 0
        pdfDoc.CurrentY = 0
        If DPI < 1 Then DPI = RENDER_DPI
        pdfDoc.RenderDPI = DPI
        Dim oPictureBox As New PictureBox
        pdfDoc.RenderPage(oPictureBox.Handle)
        GetImageFromPDF = Render(pdfDoc)
        oPictureBox.Dispose()
      End If
    Catch ex As Exception
      Throw ex
    End Try
  End Function

  Public Shared Function Render(ByRef pdfDoc As PDFLibNet.PDFWrapper) As System.Drawing.Bitmap
    Try
      If pdfDoc IsNot Nothing Then
        Dim backbuffer As System.Drawing.Bitmap = New Bitmap(pdfDoc.PageWidth, pdfDoc.PageHeight)
        pdfDoc.ClientBounds = New Rectangle(0, 0, pdfDoc.PageWidth, pdfDoc.PageHeight)
        Dim g As Graphics = Graphics.FromImage(backbuffer)
        Using g
          Dim hdc As IntPtr = g.GetHdc()
          pdfDoc.DrawPageHDC(hdc)
          g.ReleaseHdc()
        End Using
        g.Dispose()
        Return backbuffer
      End If
    Catch ex As Exception
      Throw ex
      Return Nothing
    End Try
    Return Nothing
  End Function

  Public Shared Function BuildHTMLBookmarks(ByRef pdfDoc As PDFLibNet.PDFWrapper, Optional ByVal pageNumberOnly As Boolean = False) As String

    If pageNumberOnly = True Then
      GoTo StartPageList
    End If

    If pdfDoc.Outline.Count <= 0 Then
StartPageList:
      BuildHTMLBookmarks = "<!--PageNumberOnly--><ul>"
      For i As Integer = 1 To pdfDoc.PageCount
        BuildHTMLBookmarks &= "<li><a href=""javascript:changePage('" & i & "')"">Page " & i & "</a></li>"
      Next
      BuildHTMLBookmarks &= "</ul>"
      Exit Function
    Else
      BuildHTMLBookmarks = ""
      FillHTMLTreeRecursive(pdfDoc.Outline, BuildHTMLBookmarks, pdfDoc)
      If Regex.IsMatch(BuildHTMLBookmarks, "\d") = False Then
        BuildHTMLBookmarks = ""
        GoTo StartPageList
      End If
      Exit Function
    End If
  End Function

  Public Shared Sub FillHTMLTreeRecursive(ByVal olParent As PDFLibNet.OutlineItemCollection(Of PDFLibNet.OutlineItem), ByRef htmlString As String, ByRef pdfDoc As PDFLibNet.PDFWrapper)
    htmlString &= "<ul>"
    For Each ol As PDFLibNet.OutlineItem In olParent
      htmlString &= "<li><a href=""javascript:changePage('" & ol.Destination.Page & "')"">" & Web.HttpUtility.HtmlEncode(ol.Title) & "</a></li>"
      If ol.KidsCount > 0 Then
        FillHTMLTreeRecursive(ol.Childrens, htmlString, pdfDoc)
      End If
    Next
    htmlString &= "</ul>"
  End Sub

  Public Enum SearchDirection
    FromBeginning
    Backwards
    Forwards
  End Enum

End Class


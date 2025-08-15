Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Text
Imports System.Collections.Generic
Imports NXOpen
Imports NXOpen.Assemblies
Imports NXOpen.UF

Module RenameMultibyteParts

    Dim theSession As Session = Session.GetSession()
    Dim theUfSession As UFSession = UFSession.GetUFSession()

    Sub Main()
        Dim ui As UI = UI.GetUI()
        theUfSession.Text.SetTextMode(UFText.ModeS.AllUtf8)
        ' Dim workPart As Part = theSession.Parts.Work
        ' If workPart Is Nothing Then
        '     Return
        ' End If
        ' Dim compAssembly As ComponentAssembly = workPart.ComponentAssembly
        ' Dim rootComp As Component = compAssembly.RootComponent

        ' If rootComp IsNot Nothing AndAlso rootComp.GetChildren().Length > 0 Then
        '     ProcessAssembly(rootComp)
        ' Else
        '     ProcessSinglePart(workPart)
        ' End If
    End Sub

    ' 处理整个装配体
    Sub ProcessAssembly(rootComp As Component)
        Dim ui As UI = UI.GetUI()
        Dim renamedCount As Integer = 0

        Dim allComponents As New List(Of Component)
        CollectAllComponents(rootComp, allComponents)

        For Each comp As Component In allComponents
            Try
                Dim refPart As Part = TryCast(comp.Prototype, Part)
                If refPart IsNot Nothing Then
                    If ProcessSinglePart(refPart) Then
                        renamedCount += 1
                        ' 如需组件替换，可取消注释：
                        ' comp.ReplaceComponent(newPath, Component.ReplaceComponentOption.Replace)
                    End If
                End If
            Catch ex As Exception
                ui.NXMessageBox.Show("警告", NXMessageBox.DialogType.Warning, "处理组件失败: " & ex.Message)
            End Try
        Next

        ui.NXMessageBox.Show("完成", NXMessageBox.DialogType.Information,
            "装配体处理完成。" & vbCrLf & "重命名文件数: " & renamedCount.ToString())
    End Sub

    ' 处理单个 Part 文件
    Function ProcessSinglePart(part As Part) As Boolean
        Dim ui As UI = UI.GetUI()
        Dim renamed As Boolean = False
        Dim fullPath As String = ""
        theUfSession.Part.AskPartName(part.Tag, fullPath)

        If String.IsNullOrEmpty(fullPath) Then Return False

        Dim fileName As String = Path.GetFileNameWithoutExtension(fullPath)
        Dim filePath As String = Path.GetDirectoryName(fullPath)
        Dim fileExt As String = Path.GetExtension(fullPath)

        If ContainsMultibyte(fileName) Then
            Dim cleanedName As String = RemoveMultibyte(fileName)
            If cleanedName = "" Then cleanedName = "NXFile"

            Dim newFileName As String = cleanedName & fileExt
            Dim newFullPath As String = Path.Combine(filePath, newFileName)

            If Not File.Exists(newFullPath) Then
                part.SaveAs(newFullPath)
                renamed = True
                ui.NXMessageBox.Show("重命名成功", NXMessageBox.DialogType.Information,
                   fileName & "文件名包含多字节字符，已重命名为："  & newFileName & vbCrLf)
            Else
                ui.NXMessageBox.Show("跳过", NXMessageBox.DialogType.Warning,
                    "目标文件已存在，跳过：" & vbCrLf & newFileName)
            End If
        End If

        Return renamed
    End Function

    ' 递归收集所有子组件
    Sub CollectAllComponents(parent As Component, ByRef result As List(Of Component))
        result.Add(parent)
        For Each child As Component In parent.GetChildren()
            CollectAllComponents(child, result)
        Next
    End Sub

    ' 检查是否含多字节字符
    Function ContainsMultibyte(input As String) As Boolean
        For Each ch As Char In input
            If AscW(ch) > 127 Then
                Return True
            End If
        Next
        Return False
    End Function

    ' 移除字符串中的多字节字符
    Function RemoveMultibyte(input As String) As String
        Dim sb As New StringBuilder()
        For Each ch As Char In input
            If AscW(ch) <= 127 Then
                sb.Append(ch)
            End If
        Next
        Return sb.ToString()
    End Function

    ' 必须实现
    Public Function GetUnloadOption(ByVal dummy As String) As Integer
        Return Session.LibraryUnloadOption.Immediately
    End Function

End Module

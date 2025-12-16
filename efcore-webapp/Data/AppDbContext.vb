Imports EfCoreWebApp.Models
Imports Microsoft.EntityFrameworkCore

Namespace Data
    Public Class AppDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of AppDbContext))
            MyBase.New(options)
        End Sub

        Public ReadOnly Property Todos As DbSet(Of Todo)
            Get
                Return Me.Set(Of Todo)()
            End Get
        End Property

        Public ReadOnly Property Products As DbSet(Of Product)
            Get
                Return Me.Set(Of Product)()
            End Get
        End Property
    End Class
End Namespace

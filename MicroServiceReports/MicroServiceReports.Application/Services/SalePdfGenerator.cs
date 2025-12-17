using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace MicroServiceReports.Application.Services
{
    public class SalePdfGenerator
    {
        public byte[] GenerateSaleReceipt(string saleJsonPayload, DateTime receivedAt)
        {
            // Configurar licencia (Community para desarrollo)
            QuestPDF.Settings.License = LicenseType.Community;

            // Parsear JSON
            var saleData = JsonDocument.Parse(saleJsonPayload);
            var root = saleData.RootElement;

            // Extraer datos
            long saleId = root.GetProperty("saleId").GetInt64();
            DateTime saleDate = root.GetProperty("date").GetDateTime();
            string user = root.TryGetProperty("user", out var userProp) ? userProp.GetString() ?? "N/A" : "N/A";
            string client = root.TryGetProperty("client", out var clientProp) ? clientProp.GetString() ?? "N/A" : "N/A";
            decimal total = root.GetProperty("total").GetDecimal();
            
            
            // Productos
            var products = new List<ProductLineItem>();
            if (root.TryGetProperty("products", out var productsArray))
            {
                foreach (var product in productsArray.EnumerateArray())
                {
                    products.Add(new ProductLineItem
                    {
                        Quantity = product.GetProperty("quantity").GetInt32(),
                        Description = product.GetProperty("name").GetString() ?? "Sin nombre",
                        UnitPrice = product.GetProperty("unitPrice").GetDecimal(),
                        Total = product.GetProperty("quantity").GetInt32() * product.GetProperty("unitPrice").GetDecimal()
                    });
                }
            }

            // Generar PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container => ComposeHeader(container, saleDate, client, saleId));
                    page.Content().Element(container => ComposeContent(container, products, total));
                    page.Footer().Element(container => ComposeFooter(container, total, receivedAt, user));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, DateTime saleDate, string client, long saleId)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                // Logo y título
                column.Item().BorderBottom(1).PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Border(1).Width(80).Height(60)
                            .AlignCenter().AlignMiddle()
                            .Text("Logo").FontSize(14).Bold();
                    });

                    row.RelativeItem().AlignRight().PaddingLeft(20)
                        .Text("COMPROBANTE DE VENTA")
                        .FontSize(22).Bold();
                });

                // Fecha y datos del cliente
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Fecha: {saleDate:dd/MM/yyyy}").FontSize(11);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"CI/NIT: {saleId}").FontSize(11);
                    row.RelativeItem().Text($"Razón Social: {client.ToUpper()}").FontSize(11);
                });
            });
        }

        private void ComposeContent(IContainer container, List<ProductLineItem> products, decimal total)
        {
            container.PaddingTop(20).Column(column =>
            {
                // Encabezado de tabla
                column.Item().Border(1).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.5f); // Cantidad
                        columns.RelativeColumn(5);    // Descripción
                        columns.RelativeColumn(2);    // Precio Unitario
                        columns.RelativeColumn(2);    // Importe
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5).AlignCenter()
                            .Text("Cantidad").Bold();

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5).AlignCenter()
                            .Text("Descripción").Bold();

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5).AlignCenter()
                            .Text("Precio Unitario Bs.").Bold();

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5).AlignCenter()
                            .Text("Importe Bs.").Bold();
                    });

                    // Productos
                    foreach (var product in products)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).AlignCenter()
                            .Text(product.Quantity.ToString());

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(product.Description);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).AlignRight()
                            .Text($"{product.UnitPrice:F2}");

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).AlignRight()
                            .Text($"{product.Total:F2}");
                    }
                });

                // Total
                column.Item().PaddingTop(10).AlignRight()
                    .Text($"Total Bs: {total:F2}")
                    .FontSize(14).Bold();

                // Convertir a texto
                int parteEntera = (int)total;
                int centavos = (int)((total - parteEntera) * 100);
                
                column.Item().PaddingTop(5)
                    .Text($"Son {ConvertirNumeroATexto(parteEntera)} {centavos:D2}/100 Bolivianos")
                    .FontSize(10);
            });
        }

        private void ComposeFooter(IContainer container, decimal total, DateTime receivedAt, string user)
        {
            container.AlignBottom().Column(column =>
            {
                column.Spacing(5);
                
                column.Item().PaddingTop(20).AlignRight()
                    .Text($"{receivedAt:dd/MM/yyyy - HH:mm:ss} - {user}")
                    .FontSize(9).Italic();
            });
        }

        private string ConvertirNumeroATexto(int numero)
        {
            if (numero == 0) return "Cero";
            if (numero < 0) return "Menos " + ConvertirNumeroATexto(-numero);

            string texto = "";

            // Millones
            if (numero >= 1000000)
            {
                int millones = numero / 1000000;
                texto += (millones == 1 ? "Un Millón" : ConvertirNumeroATexto(millones) + " Millones");
                numero %= 1000000;
                if (numero > 0) texto += " ";
            }

            // Miles
            if (numero >= 1000)
            {
                int miles = numero / 1000;
                texto += (miles == 1 ? "Mil" : ConvertirNumeroATexto(miles) + " Mil");
                numero %= 1000;
                if (numero > 0) texto += " ";
            }

            // Centenas
            if (numero >= 100)
            {
                int centenas = numero / 100;
                string[] nombresCentenas = { "", "Ciento", "Doscientos", "Trescientos", "Cuatrocientos", 
                    "Quinientos", "Seiscientos", "Setecientos", "Ochocientos", "Novecientos" };
                
                if (numero == 100)
                    texto += "Cien";
                else
                    texto += nombresCentenas[centenas];
                
                numero %= 100;
                if (numero > 0) texto += " ";
            }

            // Decenas y unidades
            if (numero >= 20)
            {
                string[] decenas = { "", "", "Veinte", "Treinta", "Cuarenta", "Cincuenta", 
                    "Sesenta", "Setenta", "Ochenta", "Noventa" };
                int d = numero / 10;
                int u = numero % 10;
                
                texto += decenas[d];
                if (u > 0)
                {
                    texto += " y " + ConvertirUnidades(u);
                }
            }
            else if (numero >= 10)
            {
                string[] especiales = { "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", 
                    "Dieciséis", "Diecisiete", "Dieciocho", "Diecinueve" };
                texto += especiales[numero - 10];
            }
            else if (numero > 0)
            {
                texto += ConvertirUnidades(numero);
            }

            return texto.Trim();
        }

        private string ConvertirUnidades(int numero)
        {
            string[] unidades = { "", "Un", "Dos", "Tres", "Cuatro", "Cinco", 
                "Seis", "Siete", "Ocho", "Nueve" };
            return unidades[numero];
        }

        private class ProductLineItem
        {
            public int Quantity { get; set; }
            public string Description { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public decimal Total { get; set; }
        }
    }
}

# Custom OVER expression converter

This sample defines a custom converter for an OVER clause in order to extend it with additional aggregate functions.

SQL Server Migration Assistant already supports an OVER clause in SELECT statements, but not all aggregate functions are covered. For example, the following query will not be converted by SSMA 7.3, because FIRST_VALUE function is not supported:

`
SELECT FIRST_VALUE(ProductName) OVER (ORDER BY ProductID) AS FirstProduct
FROM Sales.SalesOrderDetail;
`

Using this sample you can easily extend the list of supported functions.
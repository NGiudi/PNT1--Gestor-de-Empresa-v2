﻿using CompanyManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CompanyManager.Controllers
{
    public class StoreController : Controller
    {
        private readonly CMContext _context;

        public StoreController(CMContext context) {
            _context = context;
        }

        // Vista de listado de productos.
        public async Task<IActionResult> Index() {
            var productsList = _context.Product.Where(p => p.Stock > 0);

            foreach (Product p in productsList) {
                p.Price = p.Price - (p.Price * p.Discount / 100);
            }

            return View(await productsList.ToListAsync());
        }

        // Vista detalle de producto.
        public async Task<IActionResult> Details(int? id) {
            if (id == null || _context.Product == null) {
                return NotFound();
            }

            ProductCart productCart;
            var product = await _context.Product.FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) {
                return NotFound();
            } else {
                // que es eso?
                productCart = new ProductCart()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Quantity = 0,
                    Price = product.Price,
                    UnitPrice = product.Price,
                };
            }

            return View(productCart);
        }

        // Vistas listado de productos en el carrito.
        public IActionResult Cart() {
            var modelo = this.ProductsInCart;
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Al agregar producto en el carrito, luego de apretar en el botón agregar al carrito.
        public async Task<IActionResult> Details(ProductCart model) {
            var product = await _context.Product.Where(p => p.Id == model.Id).FirstOrDefaultAsync();

            if (product == null || _context.Product == null) {
                return NotFound();
            }

            model.SetProducto(product);

            if (ModelState.IsValid) {
                
                if (productHaveStock(model).Result) {
                    this.AddProductToCart(model);
                    return RedirectToAction(nameof(Cart));
                }

                // Error falta de stock.
                ModelState.AddModelError("Quantity", ErrorViewModel.InsufficientStork);
            }
            
            return View(model);
        }

        // Al eliminar producto del carrito, luego de apretar en el botón eliminar.
        public IActionResult DeleteProductInCart(int id) {
            var carrito = this.ProductsInCart;
            var productoExistente = carrito.Where(o => o.Id == id).FirstOrDefault();

            //Si el producto no esta, lo agrego, sino remplazo la cantidad
            if (productoExistente != null)
            {
                carrito.Remove(productoExistente);
                this.ProductsInCart = carrito;
            }

            return RedirectToAction(nameof(Cart));
        }

        // Carrito de productos.
        public List<ProductCart> ProductsInCart {
            get
            {
                var value = HttpContext.Session.GetString("Productos");

                if (value == null)
                    return new List<ProductCart>();
                else
                    return JsonConvert.DeserializeObject<List<ProductCart>>(value);
            }
            set
            {
                var js = JsonConvert.SerializeObject(value);
                HttpContext.Session.SetString("Productos", js);
            }
        }

        // Método para validar que exista el stock del producto.
        private async Task<Boolean> productHaveStock(ProductCart pCarrito) {
            var p = await _context.Product.FirstOrDefaultAsync((p) => p.Id == pCarrito.Id);

            if (p == null) {
                return false;
            }

            return (p.Stock >= pCarrito.Quantity);
        }

        // Método para agregar producto en el carrito.
        private void AddProductToCart(ProductCart pCarrito) {   
            var carrito = this.ProductsInCart;
            var pExistente = carrito.Where(o => o.Id == pCarrito.Id).FirstOrDefault();

            //Si el producto no esta, lo agrego, sino remplazo la cantidad
            if (pExistente == null) {
                carrito.Add(pCarrito);
            } else {
                pExistente.Quantity = pCarrito.Quantity;
                pExistente.Price = pCarrito.Price;
            }

            this.ProductsInCart = carrito;
        }
    }
}

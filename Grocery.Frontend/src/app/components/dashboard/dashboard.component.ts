import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ProductService } from '../../services/product.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  products: any[] = [];
  username = '';
  role = '';
  isAdmin = false;
  showModal = false;

  newProduct: any = {
    name: '',
    price: 0,
    stockQuantity: 0
  };

  constructor(
    private authService: AuthService,
    private productService: ProductService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername() || '';
    this.role = this.authService.getRole() || '';
    this.isAdmin = this.authService.isAdmin();
    
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (data) => {
        this.products = data;
      },
      error: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  openModal(): void {
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.newProduct = {
      name: '',
      price: 0,
      stockQuantity: 0
    };
  }

  saveProduct(): void {
    const payload = {
      name: this.newProduct.name,
      price: Number(this.newProduct.price) || 0,
      stockQuantity: Number(this.newProduct.stockQuantity) || 0
    };
    this.productService.addProduct(payload).subscribe({
      next: () => {
        this.closeModal();
        this.loadProducts();
      },
      error: (err) => console.error(err)
    });
  }

  deleteProduct(id: string): void {
    this.productService.deleteProduct(id).subscribe({
      next: () => {
        this.loadProducts();
      },
      error: (err) => console.error(err)
    });
  }
}

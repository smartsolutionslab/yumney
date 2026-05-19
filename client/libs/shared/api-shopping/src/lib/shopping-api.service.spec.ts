import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShoppingApiService, CreateShoppingListRequest, ShoppingListDetail, ShoppingListSummary } from './shopping-api.service';

const mockDetail: ShoppingListDetail = {
  identifier: 'list-123',
  title: 'Weekly Groceries',
  recipeIdentifier: 'recipe-abc',
  createdAt: '2026-03-10T00:00:00Z',
  items: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
  ],
};

const mockSummaries: ShoppingListSummary[] = [
  {
    identifier: 'list-123',
    title: 'Weekly Groceries',
    itemCount: 2,
    createdAt: '2026-03-10T00:00:00Z',
  },
  {
    identifier: 'list-456',
    title: 'Party Supplies',
    itemCount: 5,
    createdAt: '2026-03-11T00:00:00Z',
  },
];

describe('ShoppingApiService', () => {
  let service: ShoppingApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ShoppingApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('createShoppingList', () => {
    it('should POST to /api/v1/shopping-lists', () => {
      const request: CreateShoppingListRequest = {
        title: 'Weekly Groceries',
        items: [{ name: 'Spaghetti', amount: 400, unit: 'g' }],
        recipeIdentifier: 'recipe-abc',
      };

      service.createShoppingList(request).subscribe((result) => {
        expect(result).toEqual(mockDetail);
      });

      const req = httpTesting.expectOne('/api/v1/shopping-lists');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockDetail);
    });
  });

  describe('getShoppingLists', () => {
    it('should GET /api/v1/shopping-lists and extract items from paged response', () => {
      service.getShoppingLists().subscribe((result) => {
        expect(result).toEqual(mockSummaries);
      });

      const req = httpTesting.expectOne('/api/v1/shopping-lists');
      expect(req.request.method).toBe('GET');
      req.flush({ items: mockSummaries, totalCount: 2, page: 1, pageSize: 20 });
    });
  });

  describe('getShoppingListById', () => {
    it('should GET /api/v1/shopping-lists/:identifier', () => {
      service.getShoppingListById('list-123').subscribe((result) => {
        expect(result).toEqual(mockDetail);
      });

      const req = httpTesting.expectOne('/api/v1/shopping-lists/list-123');
      expect(req.request.method).toBe('GET');
      req.flush(mockDetail);
    });
  });
});

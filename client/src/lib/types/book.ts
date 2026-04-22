export interface Book {
  id: number;
  title: string;
  author: string;
  price: number;
}

export type BookInput = Omit<Book, 'id'>;

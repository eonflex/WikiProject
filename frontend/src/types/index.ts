export type ArticleStatus = 'Draft' | 'Published' | 'Archived';

export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;
  createdAt: string;
  updatedAt: string;
}

export interface Article extends ArticleSummary {
  content: string;
}

export interface ArticleListResponse {
  items: ArticleSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ArticleFilters {
  search?: string;
  category?: string;
  tag?: string;
  status?: ArticleStatus;
  page?: number;
  pageSize?: number;
}

export interface CreateArticleRequest {
  title: string;
  slug?: string;
  summary: string;
  content: string;
  category: string;
  tags: string[];
  status: number; // 0=Draft, 1=Published, 2=Archived
}

export interface UpdateArticleRequest extends CreateArticleRequest {}

export interface ValidationErrors {
  [field: string]: string[];
}

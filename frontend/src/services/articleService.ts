import axios, { AxiosError } from 'axios';
import type {
  Article,
  ArticleListResponse,
  ArticleFilters,
  CreateArticleRequest,
  UpdateArticleRequest,
} from '../types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5018',
  headers: { 'Content-Type': 'application/json' },
});

function buildParams(filters: ArticleFilters): Record<string, string | number> {
  const params: Record<string, string | number> = {};
  if (filters.search) params.search = filters.search;
  if (filters.category) params.category = filters.category;
  if (filters.tag) params.tag = filters.tag;
  if (filters.status) params.status = filters.status;
  if (filters.page) params.page = filters.page;
  if (filters.pageSize) params.pageSize = filters.pageSize;
  return params;
}

export const articleService = {
  async list(filters: ArticleFilters = {}): Promise<ArticleListResponse> {
    const { data } = await api.get<ArticleListResponse>('/api/articles', {
      params: buildParams(filters),
    });
    return data;
  },

  async getById(id: number): Promise<Article> {
    const { data } = await api.get<Article>(`/api/articles/${id}`);
    return data;
  },

  async getBySlug(slug: string): Promise<Article> {
    const { data } = await api.get<Article>(`/api/articles/slug/${slug}`);
    return data;
  },

  async create(request: CreateArticleRequest): Promise<Article> {
    const { data } = await api.post<Article>('/api/articles', request);
    return data;
  },

  async update(id: number, request: UpdateArticleRequest): Promise<Article> {
    const { data } = await api.put<Article>(`/api/articles/${id}`, request);
    return data;
  },

  async delete(id: number): Promise<void> {
    await api.delete(`/api/articles/${id}`);
  },

  async getCategories(): Promise<string[]> {
    const { data } = await api.get<string[]>('/api/categories');
    return data;
  },

  async getTags(): Promise<string[]> {
    const { data } = await api.get<string[]>('/api/tags');
    return data;
  },
};

/** Extracts a human-readable error message from an Axios error. */
export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const detail = error.response?.data;
    if (detail?.title) return detail.title;
    if (typeof detail === 'string') return detail;
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred.';
}

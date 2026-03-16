import { useState, useEffect } from 'react';
import type { Article } from '../types';
import { articleService } from '../services/articleService';

interface UseArticleResult {
  article: Article | null;
  loading: boolean;
  error: string | null;
}

export function useArticle(id: number): UseArticleResult {
  const [article, setArticle] = useState<Article | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    articleService
      .getById(id)
      .then(setArticle)
      .catch(() => setError('Article not found.'))
      .finally(() => setLoading(false));
  }, [id]);

  return { article, loading, error };
}

export function useArticleBySlug(slug: string): UseArticleResult {
  const [article, setArticle] = useState<Article | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    articleService
      .getBySlug(slug)
      .then(setArticle)
      .catch(() => setError('Article not found.'))
      .finally(() => setLoading(false));
  }, [slug]);

  return { article, loading, error };
}

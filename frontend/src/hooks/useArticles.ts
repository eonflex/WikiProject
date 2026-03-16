import { useState, useEffect, useCallback } from 'react';
import type { ArticleListResponse, ArticleFilters } from '../types';
import { articleService } from '../services/articleService';

interface UseArticlesResult {
  data: ArticleListResponse | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useArticles(filters: ArticleFilters = {}): UseArticlesResult {
  const [data, setData] = useState<ArticleListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const key = JSON.stringify(filters);

  const fetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await articleService.list(filters);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load articles.');
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  useEffect(() => { fetch(); }, [fetch]);

  return { data, loading, error, refetch: fetch };
}

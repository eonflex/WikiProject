import { useState, useEffect, useCallback } from 'react';
import type { ArticleStatus } from '../types';
import { useArticles } from '../hooks/useArticles';
import { articleService } from '../services/articleService';
import ArticleCard from '../components/ArticleCard';
import SearchBar from '../components/SearchBar';
import FilterControls from '../components/FilterControls';
import Pagination from '../components/Pagination';
import { LoadingSpinner, ErrorMessage, EmptyState } from '../components/StateDisplay';

export default function ArticlesPage() {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [category, setCategory] = useState('');
  const [tag, setTag] = useState('');
  const [status, setStatus] = useState<ArticleStatus | ''>('');
  const [page, setPage] = useState(1);
  const [categories, setCategories] = useState<string[]>([]);
  const [tags, setTags] = useState<string[]>([]);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Load filter options once
  useEffect(() => {
    Promise.all([articleService.getCategories(), articleService.getTags()])
      .then(([cats, tgs]) => {
        setCategories(cats);
        setTags(tgs);
      })
      .catch(() => {/* non-critical */});
  }, []);

  const { data, loading, error } = useArticles({
    search: debouncedSearch || undefined,
    category: category || undefined,
    tag: tag || undefined,
    status: status || undefined,
    page,
    pageSize: 12,
  });

  const handleReset = useCallback(() => {
    setSearch('');
    setDebouncedSearch('');
    setCategory('');
    setTag('');
    setStatus('');
    setPage(1);
  }, []);

  function handleFilterChange(setter: (v: string) => void) {
    return (v: string) => {
      setter(v);
      setPage(1);
    };
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Articles</h1>
        {data && (
          <span className="page-count">{data.totalCount} article{data.totalCount !== 1 ? 's' : ''}</span>
        )}
      </div>

      <div className="toolbar">
        <SearchBar value={search} onChange={(v) => { setSearch(v); }} />
        <FilterControls
          categories={categories}
          tags={tags}
          selectedCategory={category}
          selectedTag={tag}
          selectedStatus={status}
          onCategoryChange={handleFilterChange(setCategory)}
          onTagChange={handleFilterChange(setTag)}
          onStatusChange={(v) => { setStatus(v); setPage(1); }}
          onReset={handleReset}
        />
      </div>

      {loading && <LoadingSpinner />}
      {error && <ErrorMessage message={error} />}

      {!loading && !error && data && (
        <>
          {data.items.length === 0 ? (
            <EmptyState message="No articles match your search." />
          ) : (
            <div className="article-grid">
              {data.items.map((a) => (
                <ArticleCard key={a.id} article={a} />
              ))}
            </div>
          )}

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  );
}

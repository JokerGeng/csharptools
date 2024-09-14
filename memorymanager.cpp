class MemoryAllocator
{
   private:
    /// memory allocator
    mutable AllocatorImpl alloc_;  // bump allocator, allocator should be the first data member
                                   // (first to create, last to destroy
    static MemoryAllocator *&getStaticInstance();

   public:
    void *allocate(size_t size, uint64_t align = 8) const;
    void deallocate(void *p) const;

    template <typename T, typename... Args>
    T &allocateRef(Args &&...args)
    {
        T *obj = new (*this) T(std::forward<Args>(args)...);
        return *obj;
    }

    static MemoryAllocator &getInstance();
    static void reset();
};
